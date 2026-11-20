import type { Problem } from './types';

export class ApiError extends Error {
  constructor(public readonly status: number, public readonly problem: Problem) {
    super(problem.detail ?? problem.title ?? `Request failed (${status})`);
  }
}

export type FetchLike = (input: string, init?: RequestInit) => Promise<Response>;

/** Development identity: the API reads the member from this header until real sign-in lands. */
export function memberHeaders(memberId: string): Record<string, string> {
  return { 'X-Member-Id': memberId, Accept: 'application/json' };
}

export async function readProblem(response: Response): Promise<Problem> {
  try {
    return (await response.json()) as Problem;
  } catch {
    return { status: response.status, title: response.statusText };
  }
}

export async function getJson<T>(path: string, memberId: string, fetchImpl: FetchLike = fetch): Promise<T> {
  const response = await fetchImpl(path, { headers: memberHeaders(memberId) });
  if (!response.ok) throw new ApiError(response.status, await readProblem(response));
  return (await response.json()) as T;
}
