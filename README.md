# Dollars N Decisions

Dollars N Decisions is a first person financial literacy simulation game made in Unity for teenagers aged 13 to 19. It was built by Money Mind Studios, a team consisting of Leong Ming Hui, Schanelle Leah Jackson, Ng Kiang Hwee and Ho Rui En Raeanne, as our Year 3.1 Capstone Project for the School of InfoComm Technology (AY 2026/27 Semester 1).

The player goes through a 30 day financial cycle in a Singapore inspired neighbourhood. They earn money through work minigames, buy groceries, keep their hunger and happiness up, deal with a gambling distraction on their work computer, and try to save enough to pay rent at the end of the month.

## Why we made it

47% of Singaporean teenagers say financial independence is a major life goal, but only 34% say they save whenever they can (Visa, 2025).

We found that the problem isn't that teenagers don't know saving is important. It's that they almost never get to practise it before the money becomes real. So we built a place where they can overspend, gamble away a week of income, run out of food money, and find out what that costs without actually losing anything.

We kept the scope to the basics on purpose: budgeting, saving, telling needs apart from wants, and handling a monthly commitment. No investments, taxes or insurance, since those felt too far ahead of our target age group.

## Features

- 30 day first person simulation across 8 scenes (house, outdoor terrain, supermarket, office lobby, office, cafeteria, intro and ending)
- 3 work minigames: Bug Bash, Inbox Triage and Deadline Dash. The first few sessions run in a fixed order, after that they're randomised so players can't just repeat their favourite one
- Money, hunger and happiness all affect each other, so working non stop doesn't actually work
- Double or Nothing gambling with poker hands and a high/low round, tuned so it loses money over time
- Supermarket cart and cashier checkout, a mini fridge at home, and cafeteria food you can only buy once per office clock in
- House upgrades with multiple tiers that raise your home work multiplier and your happiness cap
- Firebase login and signup
- Tutorials for the house and the minigames

## Gameplay

Log in, set your savings goal and pick your rent, then wake up in the house. From there you travel around by SBS bus and scene doors, work to earn money, buy food, upgrade the house, gamble if you want to, and sleep to move to the next day. Survive to Day 30.

Each day is split into 7 time phases: 06:00, 09:00, 12:00, 15:00, 18:00, 21:00 and 00:00. Almost every action costs at least one phase.

### Mechanics

| System | What it does |
|---|---|
| Money | Earned from work minigames and gambling. Spent on food, cafeteria meals and house upgrades. If you go into debt you have 2 days to clear it. |
| Hunger | Drops when you work, sleep or gamble. Under 25%, actions have a 60% chance of taking two phases instead of one. Stay hungry too long and you lose. |
| Happiness | Working lowers it, sleeping and gambling raise it. Every 20 points below max costs you 5% income. The cap starts at 100 and goes up to 200 through upgrades. |
| Rent | Due on Day 30. You can choose to raise it at the start if you want a harder run. |

### Winning and losing

You win by reaching the deadline with enough money to pay rent. Whatever is left over after rent is your score.

You lose if you stay in debt too long, stay hungry too long, or reach Day 30 without enough for rent.

### Controls

| Input | Action |
|---|---|
| W A S D | Move |
| Mouse | Look around, click UI |
| E | Interact. A prompt shows up when you're near something, like `[E] Use Laptop` or `[E] Go to Sleep` |
| Esc | Exit the laptop or monitor view |

## Built with

| | |
|---|---|
| Engine | Unity 6000.2.8f1 |
| Language | C# |
| Rendering | Universal Render Pipeline 17.2.0 |
| Input | Unity Input System 1.14.2 with the Starter Assets first person controller |
| Camera | Cinemachine 3.1.2 |
| UI | uGUI, TextMeshPro, Animator |
| Backend | Firebase Authentication and Realtime Database |
| Greyboxing | ProBuilder 6.0.9 |

## Running the project

You'll need Unity 6000.2.8f1 installed through Unity Hub. Other versions might not open the project properly. Git LFS is recommended because of the 3D assets.

```bash
git clone https://github.com/Mysteryboi-07/Dollars-N-Decisions.git
```

1. In Unity Hub, click Add project from disk and pick the cloned folder
2. Open it with 6000.2.8f1. The first import takes a while since the Library folder has to be rebuilt
3. Open `Assets/Scenes/IntroScene.unity`
4. Hit Play

Always start from `IntroScene`. It sets up `GameManager` and `DatabaseManager`, which stay alive across scenes and everything else depends on. If you press Play from something like `OfficeScene` you'll get missing reference errors.

### Firebase

Firebase is already set up through `Assets/google-services.json`. If you want to point it at your own project:

1. Make a project in the Firebase console, turn on Email/Password auth and Realtime Database
2. Swap in your own `google-services.json`
3. Reopen the project so Unity regenerates the Firebase settings

We kept the database structure simple for the prototype:

```
Users
└── userId
    └── Profile
        └── Email
```

## Folder structure

```
Assets/
├── Scenes/              # The 8 game scenes
├── Scripts/             # Gameplay C#
│   └── LaptopMonitor/   # Minigames, gambling, upgrades, desktop UI
├── Models/              # 3D assets from Maya
├── Textures/            # Substance Painter texture atlases
├── Images/              # UI sprites and tutorial slides
├── Prefabs/             # Interactables and UI prefabs
├── Anims/               # Animator controllers and clips
├── MP3/                 # Audio
├── Firebase/            # Firebase SDK
└── Starter Assets/      # First person controller
```

### Scenes

| Scene | What's in it |
|---|---|
| `IntroScene` | Login and signup, savings goal and rent selection |
| `HouseScene` | Sleeping, laptop work, mini fridge, house upgrades |
| `SampleScene` | The outdoor terrain with HDBs, roads, shops and bus stops |
| `SupermarketScene` | Shelves, cart and cashier |
| `OfficeLobbyScene` | Entrance to the office |
| `OfficeScene` | Monitor work minigames and the gambling feature |
| `CafeteriaScene` | Cafeteria stores, food has to be eaten there |
| `EndingScene` | Win and lose screens |

## How the code is organised

Most of the game runs through manager scripts instead of putting logic on individual objects. We did it this way because the gameplay changed a lot during development, and it meant we could add new objects and systems without rewriting things every time.

`GameManager` is the main one. It's a singleton with `DontDestroyOnLoad` and it holds money, day, time phase, hunger, happiness, bag contents, rent, debt and the ending checks. Keeping all of that in one script made it much easier to follow what was happening when several systems affected the same value, like a minigame reward being changed by both happiness and the home work multiplier. `GameSceneUI` passes the UI references from each scene back into it.

Every interactable object in the game uses the same `InteractableTrigger` setup, so doors, the laptop, the monitor, the bed, shop shelves, the cashier, the cafeteria stores and the mini fridge all work the same way. `InteractionUIManager` handles the `[E]` prompt. When you interact with the laptop or monitor, `FocusViewManager` disables the player, switches to a dedicated camera, opens the device UI and unlocks the cursor.

Moving around is handled by `SceneTravelManager`, `SceneDoorManager` and `WaypointTravelManager`. `SleepManager` covers sleeping, naps and overnight work along with the fade transitions.

For work, `MinigameManager` sits above the three minigames and decides which one launches, so no minigame controls its own scheduling. They all implement `IWorkMinigame`.

The economy is split across `ConvenienceShopManager` for the supermarket cart and checkout, `MiniFridgeManager` for food at home, `CafeteriaManager` for office food, and `UpgradeManager` for the upgrade tiers. They all read and write through `GameManager`.

For login, `DatabaseManager` keeps the signed in user across scene loads, and `LoginPanelManager` and `SignUpPanelManager` handle the Firebase calls and error messages.

## Tweaking the balance

The balance values are serialised fields on `GameManager`, so they can be changed from the Inspector without touching code. These are the defaults:

| Value | Default |
|---|---|
| Starting money | $20 |
| Starting hunger and happiness | 100 each |
| Max happiness, base to fully upgraded | 100 to 200 |
| Time phases per day | 7 |
| Rent due day and amount | Day 30, $10,000 |
| Debt grace period | 2 days |
| Low hunger threshold | 25 |
| Chance of an extra phase when hungry | 60% |
| Low hunger grace period | 2 days |
| Happiness penalty step | every 20 points, 5% less income |
| Home work multiplier range | 0.5x to 2x |

Whatever is set in the scene Inspector overrides these at runtime.

## Known bugs

- Progress isn't saved. Firebase only stores the login and profile, so closing the game means starting the month over.
- The leaderboard isn't in yet. The score gets calculated at the ending but there's nowhere for it to go.
- Pressing Play from any scene other than `IntroScene` throws missing reference errors, since the persistent managers never get created.
- A lot of the managers rely on references assigned in the Inspector, so renaming, moving or disabling a scene object can quietly break a system. Most of the bugs we hit during development came from this, usually a missing EventSystem or an unassigned UI reference making buttons unclickable.
- Some models on the terrain overlap slightly and flicker when you walk past them at certain angles.
- Lighting isn't baked. We tried baking it but it broke a bunch of materials and took hours, so we reverted it.
- The supermarket sells food by category rather than individual products, so you can't compare prices within a category.
- The accessibility features (language options, text to speech, larger text) only exist in the Figma prototype and aren't in the Unity build.
- Some mechanics aren't explained anywhere. Happiness lowering your income and low hunger making actions take longer are both things you have to work out yourself.

## Team

Money Mind Studios, an Immersive Media student team covering programming, 3D modelling, UI/UX, visual design and marketing.

| Member | What they did |
|---|---|
| Ng Kiang Hwee (Keagan) | Lead programmer. All the Unity C# gameplay systems, Firebase integration, minigames, gambling, economy, UI systems and ending logic |
| Leong Ming Hui | 3D environment and terrain design, supermarket layout, UI assets, branding and logos, JIRA tracking |
| Schanelle Leah Jackson | UI/UX and Figma prototyping, 3D modelling for the house, supermarket, outdoor area and lobby, project documentation, marketing trailer |
| Ho Rui En, Raeanne | 3D modelling for the office and cafeteria, Figma scene layouts, marketing posters and TikTok campaign, user research and testing |

## Tools and credits

| | |
|---|---|
| Engine | Unity 6, C# |
| 3D and texturing | Autodesk Maya, Adobe Substance 3D Painter, Photoshop |
| UI and graphics | Adobe Illustrator, Figma, Canva, remove.bg |
| Video | Adobe After Effects, CapCut |
| Backend | Firebase Authentication, Realtime Database |
| Project management | JIRA, GitHub |

Unity asset packs used in the environment: 100 People Animated Characters Pack, Gogo Casual Pack, Stylized Nature Environment, AllSky Free.

## Links

- Repository: https://github.com/Mysteryboi-07/Dollars-N-Decisions
- User testing interview: https://youtu.be/LcF6VZlznpY
- TikTok: https://www.tiktok.com/@moneymindstudios_

The statistics in "Why we made it" are from Visa (2025). The full citation is in our final report.
