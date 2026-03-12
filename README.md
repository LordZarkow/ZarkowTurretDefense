# Zarkow Turret Defense

## Description

Imagine building your house, some farming area, maybe a marina or a whole village - and you get a message saying that the 'The Forest Is Moving' or 'The Ground Is Shaking'. You now know what is next is a boring session of trying to out-run and not dying to the many monsters that keep spawning, as they try to disturb your building-adventure.

Let's turn the games most boring event into something fun!

The Turret Defense addition allows you to magically build effective sci-fi turrets that can deal with monsters attacking your settlements and dig-sites.

Mod tested primarily in singelplayer game and have been designed to work in multiplayer too. But it requires confirmation.

## Version

1.0.1000

Update Jutunn to version 2.28.0

Migrate solution to VS2026


## Content

The mod features the ability to build a number of different turrets and other defense items.

Below turrets require a Workbench nearby:

* Turret Mk. I         --  2x Tin, 4x Wood
* Turret Mk. II        --  2x Copper, 4x RoundLog
* Turret Mk. III       --  2x Bronze, 4x RoundLog
* Turret Mk. IV        --  2x Iron, 4x FineWood
* Turret Mk. V         --  2x Silver, 4x FineWood
* Turret Mk. VI        --  2x BlackMetal, 4x FineWood
* Turret Mk. VII       --  2x BlackMarble, 4x FineWood
* Heavy Turret Mk. I   --  2x BlackMetal, 4x Iron, 10x FineWood
* Heavy Turret Mk. II  --  2x BlackMarble, 4x Iron, 10x FineWood
* Heavy Turret Mk. III --  2x FlametalNew, 4x Iron, 10x FineWood

* Missile Turret       --  1x SurtlingCore, 2x Bronze, 4x FineWood
* Quad Missile Turret  --  3x SurtlingCore, 3x Bronze, 4x FineWood
* Multi Missile Turret --  4x SurtlingCore, 4x Silver, 4x FineWood

* Drone Turret             --  2x SurtlingCore, 2x Bronze, 4x FineWood
* Healing Drone Turret     --  2x AncientSeed, 2x SurtlingCore, 2x Bronze, 4x FineWood
* Repair Drone Turret      --  2x AncientSeed, 2x SurtlingCore, 2x Bronze, 4x FineWood
* Gather Drone Turret      --  2x AncientSeed, 2x SurtlingCore, 2x Bronze, 4x FineWood
* Fishing Drone Turret     --  2x AncientSeed, 2x SurtlingCore, 2x Bronze, 4x FineWood
* Logger Drone Turret      --  2x AncientSeed, 2x SurtlingCore, 2x Bronze, 4x FineWood

Below turrets does NOT require a Workbench nearby:

* Signal Turret            --  10x Resin, 4x Wood
* Spotlight Turret         --  10x Resin, 4x Wood
* Mine Turret              --  2x Pukeberries, 4x Wood
* Electricity Mine Turret  --  1x TrophyHatchling, 4x Wood
* Light Turret Mk. I       --  2x Flint, 4x Wood
* Light Turret Mk. II      --  2x Bronze, 4x RoundLog

Building parts that can be built without Workbench nearby:

* Light Stick (Any color)  --  2x Wood, 4x GreydwarfEye

Building parts requiring Workbench nearby:

* Small Spotlight         --  1x Iron, 10 GreydwarfEye
* Spotlight Tower         --  3x Iron, 10 GreydwarfEye
* Tall Spotlight Tower    --  5x Iron, 10 GreydwarfEye
* Laying Iron Structure   --  5x Iron
* Standing Iron Structure --  5x Iron

Additional turrets and helper-gear to be added in future versions.

## Functionality

* A turret that is in SCAN mode, tracking a target, will show a YELLOW searchlight.
* A turret that is in ATTACK mode, attempting to destroy a target, will show a RED searchlight and TURN ON the laser-sights.

The mode of a turret can easily been seen from a distance based on the Searchlight being activated and if so what color it has. A turret that has no target to SCAN or ATTACK will idle and wait for targets to wander into their scanning-range.

* All turrets will attack any monster they deem to be a threat. The base-range is 25 with an increase in range for each higher Mk version used.

The group called ForestMonsters will be considered less of a threat and can be as close as half the attack-range before being automatically attacked, if the monster is not Alerted and for instance attacking the base or the player. For any ForestMonster that is attacking the player the full range is used. This also means a deer or boar will not be automatically attacked when it is far away and minding its own business.

* Turrets will track monsters that is up to 50% outside of their effective attack-range.
* Turrets will track Players at half their attack-effective range, but never attack them.
* Turrets will track monsters it deems to be a threat even when they are behind a wall or object, but will show a YELLOW searchlight to indicate it is scanning for them and cannot attack.
* Turrets will not attack tamed animals or animals that have a tame-rate of more than 10 out of 100.
* Turrets try to re-aquire a target every 0.40 seconds.
* Turrets use range-based prioritization to grab the target that is the closest to it and can be attack ahead of others, with several levels of de-prioritization.

Additional notes for turrets that use missiles:

* Will only track Targets that it can see, has no target-scanner.
* Missiles are guided and will track towards a Target as long as it is alive.
* If a Target is killed while the missile is in flight, it will ask the turret for a new Target and start tracking towards it.
* Some turrets may track multiple Targets and can divide its payloads towards them evenly.
* Missiles detonate upon impact and will explode in sphere towards multiple targets within range.
* An exploding missile may hit the same target multiple times if it is a Large target, such as Trolls, as they have multiple collisionmeshes (body parts).
* When the Target is more than 20 meters away the missile will take a fly-above pattern at 12 meters above for a top-down attack.
* If the Target is 'flying' the missile will not initiate a top-down attack.

## Technical details

Name: Signal Turret
Range: 20
Fire interval: 18.0

Name: Mine Turret
Range: 8
Damage, ranged: 20 Poison
Fire interval: 3.0 (DPS: 6.67)

Name: Electricity Mine Turret
Range: 6
Damage, ranged: 10 Lightning
Fire interval: 1.0 (DPS: 10.0)

Name: Light Turret Mk. I
Rotation: -120/+120
Tilt: -25/+45
Rotation/Tilt speed: 50
Range: 20
Damage, impact: 0.5 Blunt, 1.0 Pierce, 1.0 Fire
Fire interval: 0.40 (DPS: 6.25)

Name: Light Turret Mk. II
Rotation: -120/+120
Tilt: -25/+45
Rotation/Tilt speed: 50
Range: 25
Damage, impact: 1.5 Blunt, 3.0 Pierce, 3.0 Fire
Fire interval: 0.35 (DPS: 21.4)

Name: Turret Mk. I
Rotation: -120/+120
Tilt: -50/+65
Rotation/Tilt speed: 130
Range: 25
Damage, impact: 1.0 Blunt, 2.0 Pierce, 2.0 Fire
Fire interval: 0.30 (DPS: 16.67 per barrel)

Name: Turret Mk. II
Rotation: -120/+120
Tilt: -50/+65
Rotation/Tilt speed: 130
Range: 28
Damage, impact: 1.5 Blunt, 3.0 Pierce, 3.0 Fire
Fire interval: 0.27 (DPS: 27.78 per barrel)

Name: Turret Mk. III
Rotation: -120/+120
Tilt: -50/+65
Rotation/Tilt speed: 130
Range: 30
Damage, impact: 2.0 Blunt, 4.0 Pierce, 4.0 Fire
Fire interval: 0.25 (DPS: 40 per barrel)

Name: Turret Mk. IV
Rotation: -120/+120
Tilt: -50/+65
Rotation/Tilt speed: 130
Range: 31
Damage, impact: 2.5 Blunt, 5.0 Pierce, 5.0 Fire
Fire interval: 0.24 (DPS: 52.08 per barrel)

Name: Turret Mk. V
Rotation: -120/+120
Tilt: -50/+65
Rotation/Tilt speed: 130
Range: 31.5
Damage, impact: 3.0 Blunt, 6.0 Pierce, 6.0 Fire
Fire interval: 0.235 (DPS: 63.83 per barrel)

Name: Turret Mk. VI
Rotation: -120/+120
Tilt: -50/+65
Rotation/Tilt speed: 130
Range: 31.75
Damage, impact: 3.5 Blunt, 7.0 Pierce, 7.0 Fire
Fire interval: 0.2325 (DPS: 75.27 per barrel)

Name: Turret Mk. VII
Rotation: -120/+120
Tilt: -50/+65
Rotation/Tilt speed: 130
Range: 31.875
Damage, impact: 4.0 Blunt, 8.0 Pierce, 8.0 Fire
Fire interval: 0.23125 (DPS: 86.48 per barrel)

Name: Heavy Turret Mk. I
Rotation: -90/+90
Tilt: -20/+35
Rotation/Tilt speed: 35
Range: 55
Minimum range: 5
Damage, impact: 12.5 Blunt, 20.0 Pierce, 20.0 Fire
Damage, ranged: 7.5 Pierce, 7.5 Fire
Explosion range: 2.25
Fire interval: 0.80 (DPS: 65.625 + 18.75 per barrel)

Name: Heavy Turret Mk. II
Rotation: -90/+90
Tilt: -20/+35
Rotation/Tilt speed: 35
Range: 58
Minimum range: 5
Damage, impact: 13.5 Blunt, 22.0 Pierce, 22.0 Fire
Damage, ranged: 8.5 Pierce, 8.5 Fire
Explosion range: 2.5
Fire interval: 0.78 (DPS: 73.71 + 21.79 per barrel)

Name: Heavy Turret Mk. III
Rotation: -90/+90
Tilt: -20/+35
Rotation/Tilt speed: 35
Range: 60
Minimum range: 5
Damage, impact: 14.5 Blunt, 24.0 Pierce, 24.0 Fire
Damage, ranged: 9.5 Pierce, 9.5 Fire
Explosion range: 2.75
Fire interval: 0.77 (DPS: 81.17 + 24.68 per barrel)

Name: Missile Turret
Rotation: -120/+120
Tilt: -30/+60
Rotation/Tilt speed: 100
Range: 60
Minimum range: 8
Damage, impact: 2.0 Blunt
Fire interval: 1.0 (Wire-guidance limited, time counted at destruction of missile)
Ammo count: 1
Reload time: 2.0
Maximum tracked Targets: 2
Explosion range: 6.5
Damage, ranged: 25.0 Pierce, 25.0 Fire
Missile Turn Rate: 1.5
Missile velocity: 20

Name: Quad Missile Turret
Rotation: -120/+120
Tilt: -30/+60
Rotation/Tilt speed: 100
Range: 50
Minimum range: 10
Damage, impact: 2.0 Blunt
Fire interval: 0.30
Ammo count: 4
Reload time: 6.0
Maximum tracked Targets: 6
Explosion range: 6.5
Damage, ranged: 25.0 Pierce, 25.0 Fire
Missile Turn Rate: 1.0
Missile velocity: 20

Name: Multi Missile Turret
Rotation: -120/+120
Tilt: -30/+60
Rotation/Tilt speed: 100
Range: 40
Minimum range: 10
Damage, impact: 2.0 Blunt, 15.0 Pierce, 15.0 Fire
Fire interval: 0.25
Ammo count: 10
Reload time: 8.0
Maximum tracked Targets: 12
Explosion range: 1.5
Damage, ranged: 5.0 Pierce, 5.0 Fire
Missile Turn Rate: 2.0
Missile Velocity: 24

Name: Drone Turret
Patrol Range: 30
Drone Weapon Range: 15
Damage, impact: 1.5 Blunt, 3.0 Pierce, 3.0 Fire
Fire interval: 0.35 (DPS: 21.4)

Name: Healing Drone Turret
Patrol Range: 30
Drone Healing Range: 4
Healing: 1.0
Fire interval: 0.20 (HPS: 5.0)

Name: Repair Drone Turret
Patrol Range: 40
Drone Repair Range: 4
Fire interval: 4.0

Name: Gather Drone Turret
Patrol Range: 60

Name: Fishing Drone Turret
Patrol Range: 100

Name: Logger Drone Turret
Patrol Range: 75

## Localization Credits

German  --  BLUBBSON

## Legal notice

All the mod-specific code is (C)opyright Johan 'Zarkow' Munkestam/Digital Software. The turret models are commercially licensed from CGPitbull.

Public git repo for the code, without licensed assets, is at:  https://github.com/LordZarkow/ZarkowTurretDefense and the code is released under a MIT license.

If making your own mod with the same assets: To acquire your own seat-license for the turret models, visit https://assetstore.unity.com/packages/3d/vehicles/space/weapons-spaceships-pack-150720 and for the launch and impact effects, visit: https://assetstore.unity.com/packages/vfx/particles/impacts-and-muzzle-flashes-57010

If the mod is missing from your favorite mod-site, contact the author and it will most likely be added.

## Contact

For any help, support, feedback or to give words of encuragement, please join the Discord: https://discord.gg/fyqtxZt

