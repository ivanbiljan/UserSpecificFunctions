# UserSpecificFunctions
**UserSpecificFunctions** is the [TShock](https://github.com/Pryaxis/TShock) plugin that allows setting custom chat prefix, suffix and color, as well as custom TShock permissions for individual users, as opposed to groups.
Players need to have an account for this to work.

## Commands

- `/us` - Sets chat properties. Requires the `us.set` permission to run the command and set your own chat properties, and `us.setother` to set someone else's.
  * `/us prefix [username] [prefix]` - Sets the user's chat prefix
  * `/us suffix [username] [suffix]` - Sets the user's chat suffix
  * `/us color [username] [rrr,ggg,bbb]` - Sets the user's chat color
  * `/us read [username]` - Prints out the user's custom chat properties
  * `/us remove [username] [prefix/suffix/color/all]` - Removes the custom chat properties of the user, requires either the corresponding `us.remove.prefix` (`.suffix`, `.color`) permission, or the `us.resetall` permission.

- `/permission` - Allows to give or take away permissions of individual users. Requires the `us.permission` permission to run.
  * `/permission add [username] [permission1 permission2]` - Give permissions to the user. Use `!` before the permission to take away a permission granted to the user by their group, like so `!tshock.tp.spawn`
  * `/permission remove [username] [permission1 permission2]` - Remove custom permissions from the user.
  * `/permission list [username]` - Lists the user's custom permissions.

## Authors and contributors
[@IvanBiljan](https://github.com/ivanbiljan) - The Author

[@Simon311](https://github.com/Simon311) - Update to TShock 5.1.3, bugfixes, readme

## Download
The binary is available in the repo's "Release" folder.
