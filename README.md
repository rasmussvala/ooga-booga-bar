# ooga booga bar

ooga booga bar is an epic game about a group of friends going into a bar and having a good time

created in unity

## Setup

Art, audio and models are stored in Git LFS so the repo stays small.

**GitHub Desktop users:** nothing to install. Clone and open in Unity as normal.

**Terminal users:** install LFS *before* cloning, or assets arrive as text
pointer files and Unity shows everything broken:

```bash
brew install git-lfs
git lfs install
```

Already cloned without it? Run those two, then `git lfs pull`.

Unity version: **6000.6.0f1** (see `ProjectSettings/ProjectVersion.txt`).

## Layout

- `Assets/Scripts/` — game code, in the `OogaBoogaBar` assembly
- `Assets/Tests/EditMode`, `Assets/Tests/PlayMode` — tests, run from Window > General > Test Runner

Code outside `Assets/Scripts/` lands in `Assembly-CSharp` and the tests cannot see it.

## Checks

CI runs project hygiene and tests on every PR.
Run the hygiene checks locally before pushing:

```bash
./scripts/check-project.sh
```

The test workflow needs three repository secrets
(Settings > Secrets and variables > Actions):

- `UNITY_EMAIL`, `UNITY_PASSWORD` — the Unity account
- `UNITY_LICENSE` — the full contents of the `.ulf` license file

Get the `.ulf` by activating Unity Personal in **Unity Hub** locally, then reading
it off disk (macOS: `/Library/Unity/Unity_lic.ulf`). Unity dropped manual
activation for Personal licenses, so the old "upload an `.alf` to
license.unity3d.com" route no longer works.
