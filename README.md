# EconomyBot

### Description
Welcome to the repository for the Economy and management bot for the Andora server. 
This bot maintains the quality of life within all transactions and overall management of the server.
Listed below is a means of building the bot, as well as a list of commands for ease of access.

## Build

#### Windows
For windows based installation, build as you would normally utilizing VS or some alternative for .Net compilation.

#### Linux and Mac
For linux and Mac based installation, things get a bit more complex. Building will require compilation using the open-source utility Mono. 

### Requirements
After building, you will need to transfer specific runtime-required assets to their folders. 
Within the built folder, you should verify the existance of the following Directories:
	<Build_Output_Folder>/Data	-	Data folder for the application. Holds relevant data for runtime.
	<Build_Output_Folder>/Data/DB	-	Database folder for the application. Holds saved data acquired during runtime.
These are vital to runtime of the application. While building on windows should manage this automatically, it may not after building via linux.

Before running, make sure that the following files are included in the Data folder:
Andora Google App Service credentials file
TokenCredentials.json
Token Credentials should be included automatically after the build process, but will need to be supplied with the appropriate credentials.
These can be received from your Tech Lead within the server. Message them in the appropriate channels.

Once these files are within the appropriate places in the build structure, you're ready to run the application.

## User Commands
The commands in this list are broken down by the relevant module. Some commands require heightened privledges in order to function.

### DTD Module

#### /dtd job
Commit DTD for your character on a day job. This doesn't update automatically, and outputted text should be appended to character sheets manually.
**Parameters:**
The syntax for this command is as follows
> /dtd job Tool_Name, Days 

**Tool_Name**
Used to determine what kind of payout each day is worth. Your character's proficiency with this skill will be polled from your Avrae character sheet. So if this isn't correctly inputted on your sheet, it will throw an error.
Additionally, you may use "unspecified" or "none" as tools for this command.

**Days**
Number of days to commit to this job. This determines the final payout.

**Usage: **
> /dtd job Cook's Utensils, 4

This will give a calculated value of gold earned for using Cook's Utensils for four days.

#### /dtd research
Not yet implemented! Commit DTD to current research. 

#### /dtd shopping
Not yet implemented! Commit DTD to out-of-character shopping. 

#### /dtd training
Not yet implemented! Commit DTD to training.

#### /dtd foraging
Not yet implemented! Commit DTD to foraging for components.

#### /dtd crafting
Not yet implemented! Commit DTD to crafting particular items.

#### /dtd travel
Not yet implemented! Commit DTD to traveling between regions. Currently this isn't utilized within the server, but may return in the future.

### Price Check Module
#### /price
Check the price of an item by its name. Will give close options should the item not be exact in name.
**Usage: ** 
> /price Potion of Healing

## Admin Commands
Commands that require heighteneed permissions. These are used for maintenance and other such functionality.

### DTD Module
#### /dtd verify job
Deprecated command. Originally was utilized to verify the amount of gold earned from a day job. This was removed as the existing command /dtd job is more relevant.

### Price Check Module
#### /price
Functionally the same as the command without permissions. However, those with Staff role or higher can see additional information such as Market high/low values, as well as can see items of non-general category.
**Usage: ** 
> /price Potion of Healing

#### /price add
Add an item to the database.
#### /price set
Set the price of an item already within the database.
#### /price setcategory
Set the category of an item within the database. Currently, this only matters for searching purposes for players, as players are only able to see items related to the Andora General Store.

### Role Permissions Module
Used for some role permissions. Naturally, this requires maximum permissions. Will eventually be deprecated.

#### /role add
Add a role with permissions. This is used to give particular roles permissions with certain commands, but will be removed in the future.
**Usage:**
> /role add @Staff

#### /role remove
Remove a role with permissions. This is used to remove particular role's permissions with certain commands, but will be removed in the future.
**Usage:**
> /role remove @Staff

### Message Module
Handles creating reminders and repeated messages. Used primarily for announcements

#### /message add
Add a message to the queue of messages handled by this module. The channel the reply will be pasted in is the channel the command is called in, but this will be changed in the future.
**Parameters:**
The syntax for this command is as follows
> /message add Message, Time_To_Post, Recurring_Interval 

**Message**
The message to be displayed on post. Note that mentions are not automatic, and will require additional input during the command call.
Example: ***@Players*** -> ***<@&934921635948019790>*** (<@&DISCORD_ID_OF_ROLE>)

**Time_To_Post**
The time at which this post will be made. Typical syntax for this is as follows:
> 2/2/2022 12:00:00 PM

**Recurring_Interval**
The interval at which this command will recurr. For commands that do not recurr, this value should be 0.
For commands that recurr, use the following syntax:
> 7.00:00:00

This can be read as follows:
> DAYS.HOURS:MINUTES:SECONDS

**Usage: **
> /message add Don't forget Chicka loves you all <@&934921635948019790>, 2/2/2022 5:00:00 PM, 01:00:00

This will send a "Don't forget Chicka loves you all <@&934921635948019790>" message every hour starting at 5pm on 2/2/2022.

#### /message remove 
Remove a message held in the message queue. This requires specific knowledge of GUID of a message, which can either be looked up manually in the files or via the debug dump command.
**Usage: **
> /message remove 1feabcf9-1217-4a40-89ff-6f9db918e74a

### NPC Ping Service Module
Used for tracking pings by certain roles. This is most notably used for tracking NPC pings within the server.

#### /ping_svc watch
Adds a channel to the watched channel list for this service. Any pings for the specific role that show up here will be managed.

**Usage: **
> /ping_svc watch #redhold

#### /ping_svc rwatch
Removes a channel from the watched channel list for this service.

**Usage: **
> /ping_svc rwatch #redhold

#### /ping_svc role
Set a role in which will be observed for pings in relevant channels within this server.

**Usage: **
> /ping_svc role @NPC

#### /ping_svc report_channel
Set a channel in which detailed data will be posted related to the ping. This is used to designate a channel for posting to.

**Usage: **
> /ping_svc report_channel #npc-pings-requests

### Debug Module
These commands are for testing purposes only, requiring maximum permission. They should not be utilized by normal users.

#### /econ_debug dump_character_db
Dump the database of characters to a log file and send that file to the calling user. 
This is mainly used for debugging currently-cached character data on the bot. 

#### /econ_debug dump_item_db
Dump the database of items and/or price data to a log file and send that file to the calling user. 
This is mainly used for debugging stored price data on the bot. 

#### /econ_debug dump_message_db
Dump the database of queued messages to a log file and send that file to the calling user. 
This is mainly used for debugging the message module maintained by the bot.

