using System.Collections.Concurrent;
using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;

namespace Huddle.Groups;

/// <summary>Thread-safe in-memory store for people and groups (persistence arrives in slice 2 behind the same interfaces).</summary>
internal sealed class InMemoryGroupStore : IGroupDirectory, IGroupRegistry
{
    private readonly ConcurrentDictionary<string, PersonView> _people = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Group> _groups = new(StringComparer.Ordinal);

    public void RegisterPerson(string memberId, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        _people[memberId] = new PersonView(memberId, displayName.Trim());
    }

    public void CreateGroup(string groupId, string name, string currency, string schedule, Money monthlyContribution)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Money.IsValidCurrency(currency) || monthlyContribution.Currency != currency)
        {
            throw new ArgumentException("A group saves in one ISO currency.", nameof(currency));
        }

        if (!_groups.TryAdd(groupId, new Group(groupId, name, currency, schedule, monthlyContribution)))
        {
            throw new InvalidOperationException($"Group {groupId} already exists.");
        }
    }

    public void AddMember(string groupId, string memberId, MemberRole role, Money contributionLimit, DateOnly joinedOn)
    {
        var group = _groups.TryGetValue(groupId, out var g) ? g : throw new KeyNotFoundException($"Group {groupId} does not exist.");
        var person = FindPerson(memberId) ?? throw new KeyNotFoundException($"Person {memberId} is not registered.");
        if (contributionLimit.Currency != group.Currency || !contributionLimit.IsPositive)
        {
            throw new ArgumentException("The contribution limit must be positive and in the group's currency.", nameof(contributionLimit));
        }

        group.Upsert(new MemberView(memberId, person.DisplayName, role, contributionLimit, joinedOn));
    }

    public PersonView? FindPerson(string memberId) => _people.TryGetValue(memberId, out var person) ? person : null;

    public GroupView? FindGroup(string groupId) => _groups.TryGetValue(groupId, out var group) ? group.View() : null;

    public Membership CheckMembership(string groupId, string memberId)
    {
        if (!_groups.TryGetValue(groupId, out var group))
        {
            return Membership.GroupNotFound;
        }

        return group.Has(memberId) ? Membership.Member : Membership.NotAMember;
    }

    /// <summary>The member's groups, the one they joined first at the top (their "home" group).</summary>
    public IReadOnlyList<GroupView> GroupsOf(string memberId) =>
        _groups.Values.Where(g => g.Has(memberId)).Select(g => g.View())
            .OrderBy(g => g.Members.First(m => m.MemberId == memberId).JoinedOn)
            .ThenBy(g => g.Name, StringComparer.Ordinal)
            .ToList();

    private sealed class Group(string id, string name, string currency, string schedule, Money monthly)
    {
        private readonly object _gate = new();
        private readonly List<MemberView> _members = [];

        public string Name { get; } = name;

        public string Currency { get; } = currency;

        public bool Has(string memberId)
        {
            lock (_gate)
            {
                return _members.Exists(m => m.MemberId == memberId);
            }
        }

        public void Upsert(MemberView member)
        {
            lock (_gate)
            {
                _members.RemoveAll(m => m.MemberId == member.MemberId);
                _members.Add(member);
            }
        }

        public GroupView View()
        {
            lock (_gate)
            {
                return new GroupView(id, Name, Currency, schedule, monthly, _members.ToArray());
            }
        }
    }
}
