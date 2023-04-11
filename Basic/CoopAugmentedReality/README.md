# com.unity.multiplayer.samples.coop-ar
 AR Multiplayer sample.

*Run on Android.
Start game, first player Click Host.
In main screen tap on a detected plane to place the table. 
Tap on a ball to select it(turns red), then tap a second time on the table or cup to move a ball (moves and turns back to original color).

Second player, enter the client code and click the client button. 
In main screen tap on a detected plane to place the table. 
Tap on a ball to select it(turns red), then tap a second time on the table or cup to move a ball (moves and turns back to original color).

---------------------------------------------------------------------------------------------------------
TODO:

Physics is currently off. Can only move a ball on the table, or onto the cup but it will stay there.

AR IS working with Relay. It is working for first player(host) for selecting balls, changing their color, moving the ball and changing back the color. 

It is not currently working for second player: 
- Currently the ball selection works, changing the color to red.
- The ball movement is not working for the second player, need to verify the ownership settings on the gameobject's to allow movement.
- Need to test it for a third player.
- Need to remove the player cylinders from the scene.
- Need to turn on physics and get that working.
- Tidy up the code.