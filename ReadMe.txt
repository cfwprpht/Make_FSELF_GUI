** MAKE FSELF GUI **

A Grafical User Interface for flat_z make_fself.py script.

* Can use a database for the authentication informations.
* TreeView.
* Batch Mode.
* Use dynamical Paths for the sript and for the db.
* You can also drop the db or the .py script onto the app and it will sabe the paths.
* Simple and Advanced mode.
* Can hexify the Auth Info. (Not that this would be anything importend, just for fun ^^)
* Hase Context Menus.
* Remember Paths.
* Can Drag And Drop.
* Error Output of the .py script is redirected.

You can add your own keys to the DB and the according fw, simple by adding two new tags like this:

[Name=New Application]
[Auth=New Auth Info]

Make sure to have the Authentication Information in one Block without spaces and non Hexifyed. So no '0x'.

Done, Have Fun !

Changelog: v1.1
Corrected a bug in the db reading routine which would cause the reading process to stop if there is a second app entry for a fw version.
Added "Only Fake Sign" mode which just use standart make_fself.py settings without any change.
Added the ps2-emu Authentication Information base (0x0000 for decimal) to the database.

v1.3
Changed make_fslef.py to make_fself.exe from xDPx should now work for a bigger range of users. :)
Added a Clear Settings Function.

 -cfwprpht-