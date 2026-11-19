#!/usr/bin/env bash
# Regression suite for the gh-pr-review scripts. The harness, its helpers and the cases for the shared
# scripts live in .agents/skills/_shared/tests; this file names the skill and its body-script floor.

SKILL_TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# The number of workers that post body text, which test_every_prose_script_calls_its_guard must
# find. It guards the derivation in that test against selecting nothing.
BODY_SCRIPT_FLOOR=5
source "${SKILL_TEST_DIR}/../../_shared/tests/harness.sh"
