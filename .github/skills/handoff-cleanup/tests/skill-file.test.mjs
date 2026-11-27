import { test } from "node:test";
import assert from "node:assert";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const skillFile = join(dirname(fileURLToPath(import.meta.url)), "..", "SKILL.md");
const md = readFileSync(skillFile, "utf8");

function frontmatter(text) {
  const match = text.match(/^---\n([\s\S]*?)\n---\n/);
  assert.ok(match, "skill file must open with a --- frontmatter block");
  return match[1];
}

test("frontmatter declares handoff cleanup", () => {
  const fm = frontmatter(md);
  assert.match(fm, /^name:\s*handoff-cleanup\s*$/m);
  assert.match(fm, /^description:\s*.+/m);
});

test("skill distinguishes removable and retained handoffs", () => {
  assert.match(md, /\*\*Remove\*\*/);
  assert.match(md, /\*\*Keep\*\*/);
  assert.match(md, /\*\*Investigate\*\*/);
  assert.match(md, /pending gate|proposal-only|incomplete implementation/i);
});

test("skill requires source verification and durable documentation", () => {
  assert.match(md, /Source and executable evidence take precedence/i);
  assert.match(md, /actual validation/i);
  assert.match(md, /continuing record/i);
  assert.match(md, /update links/i);
});

test("skill forbids unsafe cleanup and artifact deletion", () => {
  assert.match(md, /Do not treat.*filename/i);
  assert.match(md, /Do not delete source examples or test fixtures/i);
  assert.match(md, /no source, tests, fixtures, or pending plans were[\s\S]*deleted accidentally/i);
});

test("artifact has no placeholder markers", () => {
  const banned = ["TO" + "DO", "FIX" + "ME", "place" + "holder", "X" + "X" + "X"];
  for (const marker of banned) {
    assert.doesNotMatch(md, new RegExp("\\b" + marker + "\\b", "i"));
  }
});
