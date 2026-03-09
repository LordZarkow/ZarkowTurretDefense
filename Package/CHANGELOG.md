- Version 0.32.960
Update Jutunn to version 2.21.3

- Version 0.31.950
Reduced material cost of Lighting Mine from 2 to 1 of TrophyHatchling.
All turrets and pieces adjusted to Hard Wood or higher as material-type declaration due to new Ashland fire mechanics.
Added Heavy Turret Mk. II.
Added Heavy Turret Mk. III.
Added Turret Mk. VI.
Added Turret Mk. VII.
Adjusted texture for Heavy Turret Mk. I.

- Version 0.30.930
Gather Drone now has a Container that can be used to define what to NOT gather.

- Version 0.29.920
LoggerDrone no longer cuts down saplings, it will let them grow up first.
All turrets and pieces now marked as immune to fire.
Bumped Jotun version to match needs for Ashland release.

- Version 0.28.910
Added Logger Drone Turret.
Increased range of Gather Drone from 50 to 60.
Deer is no longer targeted by turrets.
Missile turrets and Heavy Turret will now damage tree, treelog, stump and stones.
All drone turrets beyond the normal guard drone now does the standby patrol very close to the turret.
Added two new Debug Settings that by default will hide despawn and heightmap missing reporting log lines.

- Version 0.27.760
Fixed Place and Destruction effects as well as Resistances that fell away in the Valheim version-push adjustment.
Increased Fishing Drone range from 60 to 100.
Added Warning-msg to be able to trace the Turrets that should be rebuilt in your world, when spotted.

- Version 0.26.720
Updated dependencies for new Valheim patch 0.217.28
Added Cost Modifier setting, to adjust the material cost per turret and building part.
Updated German Localization.
Turrets that does not require workbench can now be built in dungeons again.

- Version 0.25.710
Added Fishing Drone Turret
Added global config 'Turrets Should Fully Ignore Players' to optionally turn off non-Healing turrets from noticing players.
Fixed so drones in patrol-move no longer go under water-surface if over a deep ocean.
Buildingparts, such as Light sticks, can now be built in dungeons.

- Version 0.24.680
Gather Drone no longer grab fishes from the sea.
Reworked all sound-handling, will overall increase sound-levels over distance but also reduce maximum range the game has to evaluate if to play it or not.
Lightsticks are no longer active targets for monsters, will hopefully be ignored now to allow marking paths through forrests.

- Version 0.23.660
Added Gather Drone Turret
Repair Drone will now patrol in a range increased from 30 to 40.

- Version 0.22.640
Added LOD-handling for all Pieces, with partial reduction in complexity for some over distance and complete removal at long distance for all.
Fix possible situations where a drastic frame-drop could cause Heavy Turrets to run out of projectiles by forcing minimum separation of half a ROF-slice.
Rewrote the projectile-penetration to remove issues with repeat hits on same location when target was not moving enough.
Optimized the colMesh filter for damage over distance checks to reduce the load on the function.
Added setting 'Damage Modifier' to optionally adjust the damage Turrets does, either up or down.
Slightly increased the Heavy Turret targeting rangre from 50 to 55.
Decreased the ROF of Heavy Turret from 0.4 to 0.8.
Slightly increased damage of Heavy Turret per shot.

- Version 0.21.600
Added alternative colored Light Sticks.
Drones no longer has a col-mesh, will stop them interacting with Ships or Players when traveling.
Repair Drone can now repair the Cart.
Turret max-tilt down is increased from 40 to 50 degrees.
Heavy Turret max-tilt down is increased from 15 to 20 degrees.
Poison and Electricity mine texture reworked to be more detectable at distance for reference to Vikings.
Shader correction for all Pieces, allowing for shadows, snow and rain effects, normal mapping and shiny metallic surface.
Adjustment to handling of Heavy Turret projectiles to avoid situations where there is none available to launch.
Decreased Heavy Turret rotation speed from 45 to 35.
Heavy Turret projectile speed decreased from 500 to 100.

- Version 0.20.550
Added Repair Drone Turret.
Improved Target handling for Drones.
Adjusted Texture and Icons for Drones.
Healing Drone will now heal Tamed animals too.

- Version 0.19.515
Added Spotlight Turret.
Added Light Stick buildingpart.
Adjusted some material cost and type on some pieces.
Reload-times for all Missile Turrets have been increased.
Fixed MineTriggerEffect for (Poison) Mine Turret.
Adjusted Red Light for turrets to be slightly brighter and slightly more red color.

- Version 0.18.505
Added German localization.
Added three configurable options of performance tweaks - DisableTurretLight, DisableDroneLight and DisableBuildingpartsLight.
Implemented usage of setting TurretVolume.
Added usage of Audioman audio mixer group by Audio Sources.

- Version 0.17.500
Added support for adding non-turrent building parts.
Added building part Small Spotlight.
Added building part Spotlight Tower.
Added building part Tall Spotlight Tower.
All turret icons updated.
Added support for localization - English.json also published on Discord.

- Version 0.16.490
Drone Turrets now ignore LOS-issues between turret and target, as it is handled by Drone.
Healing Drone Turret now looks for Players in the full patrol range.
Updated texture for Healing Drone.
Increased the health of all turrets.
Set placement and destruction effect for all turrets.
Adjust resistance for all turrets.

- Version 0.15.480
Added Electricity Mine Turret.
Added Healing Drone Turret.
All damages towards Players are now set at 25%, with Stun-multiplier as if damage was at 100%.

- Version 0.14.465
Added Light Turret Mk. II.
Improve target-status validation for Drone Turret.
Missile Turrets now have a 0.25-0.75s re-targeting delay after monster-death to allow for more organic animation of missiles.
Improved handling for MoveAndAttackTarget() for Drone.

- Version 0.13.450
Added Drone Turret.
Added remote-data synchronisation for all turrets.
Signal Turrets are no longer priority targets for monsters.
Additional turret-shared code improved to be more reusable.

- Version 0.12.400
Textures are now of pixelated style to fit the Valheim theme.
All turrets are now organized on a new build-page called 'Turrets'.
Code prepared for next type of turret to be added.

- Version 0.11.380
Added Heavy Turret Mk. I.
Slightly reduced the damage for Multi Missiles.
Bumped up dependency version to ValheimModding-Jotunn-2.12.4.
Confirmed compatibility with "Hildir's Request Update".

- Version 0.10.365
Added Multi Missile Turret.
Added Mine Turret.
Implemented usage of Valheim Piece Shaders on all built turrets.
Added support for hold-pattern for missiles that lose their tracked target.
Added trail-effect to all missiles.
Some adjustements to Missile and Quad Missile turret stats.

- Version 0.9.270
Added Quad Missile Turret.
Added Turret Mk. V.
Huge refactoring to allow for multi-targeting turrets.
Turret Mk. III material requirement changed from 'FineWood' to 'RoundLog'.
Increased Missile Turret range from 60 to 80.
Increased Missile Turret Missile turn-rate from 1.10 to 1.50.
Maximum distance for a missile to travel is now 1.5 times max targeting range from turret base.
Added detailed info section in readme file.

- Version 0.8.215
Added Turret Mk. IV.
Missile Turret now has a Bronze color-shade on main body.
Code refactoring started to enable more diverse turrets in the future.

- Version 0.7.200
All gun turrets got some Fire damage added and their Blunt damage reduced by 50%.
Missile explosion had Fire damage added and pierce damage increased. Range increased but damage falloff increased.
Missile Explosion does -90% in damage against players for now, up from -100%.

- Version 0.6.193
Added Missile Turret - cable-controlled missile, long range and high chance to stagger the monsters caught in the blast.
Increased impact damage for Turrets Mk.I-Mk.III.
Increased range for Signal Turret from 15 to 20 meters.

- Version 0.5.145
Added Light Turret.
Fixed negative scaling of some turret parts that lead to negative size-warning on creation of destroyed piece fragments.

- Version 0.4.140
Turrets now to 50% blunt damage and 50% piece damage.
Turrets bullet-impacts has increased stagger-ratio.
Turrets now have number of antennas based on Mk. type.
Adjusted tint of Turret body based on metallic material used.

- Version 0.3.130
Added handling of health and destruction of Signal Turret.

- Version 0.2.126
Added Signal Turret.

- Version 0.1.105
Initial Release.
Turret Mk.I, Turret Mk.II and Turret Mk.III added.