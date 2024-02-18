# Zelda_Assassin_Climb_System
A prototype-rework of an old school project focused on making a dynamic, walking/climbing system that fuses the climbing mechanics of the Assassin's Creed games and the latest Zelda games

The project makes use of a custom finite-state machine, inheritance, and polymorphism to support different types of movement mechanics.
The basics of the movement mechanics are divided into 3 main parts:
1. The PlayerController, which controls the state-machine

![PlayerController](/Images/PlayerController.png)

2. & 3. The IPlayerMovement and PlayerMovementBase, both of which are the foundation of the inheritance and polymorphism of this system, enabling further movement mechanics to be easily adjusted and extended

Branching of off that, and what makes up the major functional part of this system are the following:
* The PlayerClimbMovement, which is the common base for how the Zelda-like and Assassin-like climbing of this project works and enabled me to segregate some common functionality between the two of them
* The PlayerMovement, which as the name sort of implies, handles the player's grounded movement and supports:
  * Walking on slopes, which also takes into account the angle of the slope and which way the player is going along the slope -- making it slower to walk up, and faster to walk down
  * "Coyote-time", allowing the player a brief window after going off an edge to still jump
  * "Mario-jump", a jump that affects the player's gravity depending on how long the jump button is held

![GroundedPlayerMovement](/Images/GenericGroundedPlayerMovement.png)
* The AssassinClimb, which as the name implies takes inspiration from the climbing system in the Assassin's Creed games, specifically the point-to-point aspect of the climbing  

![AssassinClimb](/Images/Assassin-likePlayerClimbing.png)
* The ZeldaClimb, which as the name implies takes inspiration from the more free surface-climbing aspect of the latest Zelda games, this also handles climbing on spherical or less flat surfaces  

![ZeldaClimb](/Images/Zelda-likePlayerClimbing.png)

Here are some gifs highlighting most of the core functionality of the system.

# Grounded movement highlights
Basic grounded movement + varying height jumping

![Grounded movement and varying height jumps](/Gifs/Ground_Move_High_Jump.gif)  
Grounded movement into "coyote" jump

![Grounded into coyote jump](/Gifs/Ground_Coyote_Jump.gif)  
Grounded slope movement

![Grounded slope movement](/Gifs/Ground_Slide_Move.gif)  

# Zelda-climb highlights
Basic Zelda-climb on straight/flat surface, i also included an upwards "wall jump" and jumping off the wall back to the ground

![Zelda climb](/Gifs/Zelda_Climb.gif)  
Basic climbing on a spherical surface

![Spherical climb](/Gifs/Spherical_Climb.gif)  
Zelda-climb on an angled surface

![Zelda angle climb](/Gifs/Zelda_Angle_Climb.gif)  
Zelda-climb, performing transitions between differently angled surfaces on a wall, also included transitioning from climbing to walking on top of a wall

![Zelda corner transition climb to walk](/Gifs/Zelda_Corner_Transition_Climb_To_Walk.gif)  
Zelda-climb, performing transitions into different climbing styles from a straight/flat surface, to a spherical surface, back to straight/flat surface

![Zelda to spherical climb](/Gifs/Zelda_To_Spherical_Climb.gif)  

# Assassin-climb highlights

![Assassin climb](/Gifs/Assassin_Climb.gif)  
"Basic" Assassin-esqué point-to-point climbing, with functionality for climbing along a longer ledge

As it is merely a prototype, it is quite functional but requires some tweaking and smoothing out the edges before it can fully be used in any capacity. More of a proof-of-concept 😜

The old version of the project can be found [here](https://github.com/Zenzyde/Free-Point_Climbing_System).
