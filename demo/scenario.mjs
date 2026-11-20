// Scripted walkthrough for the README demo. Run: node demo/record.mjs .
export default async ({ page, step, shot }) => {
  await step("Huddle: a stokvel group's shared balance, summed from ledger entries", async () => {
    await page.getByText("group balance").first().waitFor();
  });
  await shot("group");
  await step("Every member and who has paid this month", async () => {
    await page.getByRole("heading", { name: /Members/ }).scrollIntoViewIfNeeded();
  });
  await step("Contribute: the amount is checked against the member's limit", async () => {
    await page.evaluate(() => window.scrollTo({ top: 0, behavior: "smooth" }));
    await page.getByRole("button", { name: "Contribute", exact: true }).click();
    await page.getByRole("textbox", { name: "Amount" }).fill("250.00");
  });
  await shot("contribute");
  await step("One idempotency key per attempt, so a retry can never pay twice", async () => {
    await page.getByRole("button", { name: /^Contribute R/ }).click();
    await page.waitForTimeout(1200);
  });
  await step("The balance and history update from the new ledger entries", async () => {
    const close = page.getByRole("button", { name: /Done|Close/ });
    if (await close.count()) await close.first().click();
    await page.getByText("Contribution history").scrollIntoViewIfNeeded();
  });
  await shot("history");
  await step("A non-member can't see the group at all", async () => {
    await page.evaluate(() => window.scrollTo({ top: 0, behavior: "smooth" }));
    const select = page.locator("label.view-as select");
    const values = await select.locator("option").allTextContents();
    const outsider = values.find(v => /Bongani/.test(v)) ?? values[values.length - 1];
    await select.selectOption({ label: outsider });
    await page.waitForTimeout(1200);
  });
  await shot("not-a-member");
};

export async function mobile({ page, shot }) {
  await page.getByText("group balance").first().waitFor();
  await shot("mobile");
}
