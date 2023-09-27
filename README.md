Readme.txt for Darkstar v1.1.9.0:


1.0 Introduction and Overview
=============================

Darkstar provides emulation of the Xerox Dandelion workstation, commonly known
as the Star, 8010, or 1108. 

To avoid confusion in the rest of this document, the name "Star" will be
used to refer to any of the above machines.

1.1 What's Emulated
-------------------

Darkstar currently emulates the following Star hardware:
   - Standard 8010/1108 Central Processor (CP) with 4KW of microcode store
   - i8085-based IO Processor (IOP)
   - Up to 768KW of main memory
   - Bitmapped Display
   - Keyboard / Mouse
   - 10, 40, or 80mb hard drives (SA1000 interface)
   - 8 inch floppy drive
   - 10mbit Ethernet
   - Real-time clock
   - Keyboard beeper

1.2 What's Not
--------------

At this time, the below are not emulated by Darkstar:
   - Serial ports
   - The LSEP printer interface

2.0 Requirements
================

Darkstar will run on any Windows PC running Windows Vista or later, with version
4.5.3 or later of the .NET Framework installed.  .NET should be present by 
default on Windows Vista and later; if it is not installed on your computer it
can be obtained at https://www.microsoft.com/net.

Darkstar will also run under Mono (http://www.mono-project.com/) on Unix 
platforms.  macOS support will be present in a future release.  SDL 2.0 is used
for the emulated display -- use your operating system's package manager to 
ensure this is installed.

The Star keyboard has many keys not present on modern keyboards.  Many of 
these are mapped to Function keys, arrow keys, and the Home/End/PgUp/PgDown 
keys present on most desktop keyboards -- laptop keyboards may be more 
difficult to use, depending on your keyboard's layout.

A three-button mouse is useful for using some Star software (XDE and 
Interlisp-D, for example).  On most mice, the mousewheel can be clicked to 
provide the third (middle) button.  Laptops with trackpads may have 
configuration options to simulate three buttons but will likely be clumsy to 
use.

If you wish to make use of the emulated Star's Ethernet interface, you
will need to have WinPCAP installed (on Windows) or libpcap (on the Unix of 
your choice).  WinPCAP can be downloaded from http://www.winpcap.org/.    
Using the Ethernet interface allows to access services on the network through
the protocols supported by the guest operating system running in Darkstar. These
can be TCP/IP based services like an FTP server or XNS (Xerox Network System)
services. Real Xerox XNS server installations are rather seldom today, so
using an XNS emulation like Dodo (see https://github.com/devhawala/dodo for
setup and configuration details) may be an alternative for building up a virtual
Xerox machines network.    
If only Dodo emulated XNS services are to be accessed from Darkstar, a direct
connection to the Dodo NetHub can be configured instead of using a pcap device
for accessing a real network.


3.0 Getting Started
===================

Installation of Darkstar on Windows is simple:  Double-click the installer 
file, named "DarkstarSetup.msi" and follow the on-screen instructions.  The 
installer will install all of the necessary files and create two icons on your 
Start menu, one for Darkstar itself, and one for its documentation (the file 
you're reading now!)

On Unix platforms, extract the Darkstar-mono.zip archive in a directory of your
choosing.


3.1 Using Darkstar
==================

On Windows, Darkstar can be started by clicking on the "Darkstar" icon created 
by the installer.  On Unix, Darkstar can be started from a shell prompt by 
running "mono Darkstar.exe" in the directory chosen in Section 3.0.

Once started, the main Darkstar window will appear.  This window
is your primary means of interaction with the emulated Star workstation.  


3.1.1 The Display
-----------------

The large (initially black) area is the Star's display.  Clicking anywhere in 
this area while the Star system is running will "capture" the mouse and 
keyboard: your mouse movements and keyboard inputs will be sent to the Star, 
and mouse movements will be restricted to the Darkstar window. To release the 
capture, press either "Alt" key on your keyboard.


3.1.2 The Status Bar
--------------------

At the bottom of the Darkstar window is the Status Bar.  This shows information
about the system.  From left to right:

- The current MP (Maintenance Panel) code.  On a real Star, this is a 4 digit
  LED display on the front of the CPU unit.  The number displayed is used to 
  communicate boot status and diagnostic information to the user.  If the 
  display reads "----" this indicates that the Star has turned the MP display 
  off or it has not been initialized.  A comprehensive list of MP codes can be 
  found on Bitsavers at 
  http://bitsavers.org/pdf/xerox/8010_dandelion/Dandelion_MPCodes_Mar85.pdf.

- The System status: Stopped or Running.

- The Emulation speed: In fields per second and as a percentage of a real 
  Star's execution speed.  78 fields/sec is approximately 100%.

- Mouse Capture status: Indicates whether the mouse is currently captured.


3.1.3 The System Menu
---------------------

The System menu allows you to control the Star system and the emulator.  The
items in the menu are enumerated below.

Start / Stop - This will start the Star system running if it is stopped, and
        stop it if it is already running.

Reset - This will reset the Star.  This is equivalent to pressing the "B Reset"
        button on a real Star.

Alternate Boot - Allows selection of an alternate boot device.  On a real Star,
        this is accomplished by holding down the Alt Boot button during
        power-up until the appropriate code appears in the MP display.
        Selecting a device in this menu will simulate holding the Alt Boot 
        button as appropriate to select the boot device.    
        In general you won't need to change this unless you are installing or
        performing maintenance on an operating system.  However: Selecting
        "Rigid" rather than the default ("DiagnosticRigid") can save time
        when booting ViewPoint or XDE.

Floppy Disk - Allows loading or unloading of floppy disk images.  If an image
        is currently loaded, its name will be displayed in the space at the
        bottom of the submenu; hovering over this space will show the full 
        path to the image and image metadata, if present.  Darkstar uses
        floppy disk images in ImageDisk (.IMD) format.
        See: http://www.classiccmp.org/dunfield/img/index.htm for details.

Hard Disk - Allows loading or creating new hard disk images, which typically
        have a ".IMG" file extension. If an image is currently loaded, its 
        name will be displayed in the space at the bottom of the submenu; 
        hovering over this space will show the full path to the image.
        See Section 10.0 for information on the image format.

Configuration - Invokes the Configuration dialog.  See Section 4.0 for more
        details on configuration options.

Full Screen - Toggles Full Screen mode, in this mode the Star's display will
        expand to fill the screen.  Press Ctrl+Shift+F to exit Full Screen
        mode.

Show Debugger - Invokes the Debugger interface.  See section 5.0 for more
        details on care and feeding.

Exit - Quits Darkstar.  Contents of loaded hard disk images are saved to the
        image files they were loaded from.


3.2 The Keyboard
----------------

The Star's keyboard has many keys that are not present on a standard PC 
keyboard.  Darkstar maps F1-F12, the arrow keys, and the home/end/pgup/pgdown
keys to these special keys, as below:

Star Key|PC Key
--------|------
Again|F1
Delete|F2
Find|F3
Copy|F4
Same|F5
Move|F6
Open|F7 or Left Control 
Props|F8 or Right Control
Center|F9
Bold|F10
Italics|F11
Underline|F12
Superscript|Print Screen
Subscript|Scroll Lock
Larger/Smaller|Pause
Defaults|Num Lock
Skip/Next|Home
Undo|Page Up
Defn/Expand|End
Stop|Page Down
Help|Up Arrow
Margins|Left Arrow
Font|Backslash
Keyboard|Down


3.3 Software
------------

3.3.1 Getting Software and Documentation
----------------------------------------

Darkstar does not come with any media.  Bitsavers has floppy disk sets for 
ViewPoint, XDE, and Interlisp-D at:

http://bitsavers.org/bits/Xerox/8010/
and
http://bitsavers.org/bits/Xerox/1108/

These can be used to bootstrap a fresh installation onto a virtual hard disk.
Note that at this time, only floppy disk images in ImageDisk format (.IMD) are
supported by Darkstar.

Pre-built hard disk images suitable for use with Darkstar are available at:

http://bitsavers.org/bits/Xerox/8010/8010_hd_images.zip


Documentation for the above operating systems is available at:

http://bitsavers.org/pdf/xerox/viewpoint
http://bitsavers.org/pdf/xerox/interlisp-d/
and
http://bitsavers.org/pdf/xerox/xde/


3.3.2 Booting from a Hard Disk
------------------------------

If you have an existing hard disk image, you can boot from it by first loading 
the image using the "System->Hard Disk->Load..." menu.  This will present a 
file dialog allowing you to select the image to load.

After the image is loaded, use the "System->Start" menu to start the Star
running.  During boot, the MP code displayed in the lower-left corner of the
window will display various values indicating status, or in the cases of
failure, a diagnostic code.

ViewPoint and XDE will run a lengthy set of diagnostics during boot -- these
can be skipped by selecting "Rigid" from the "System->Alternate Boot" menu
before starting or restarting the Star.


3.3.3 Installing an Operating System
------------------------------------

Covering the proper installation and maintenance of the various Star operating
systems is beyond the scope of this manual, but a few poorly documented and
emulator-specific bits of advice are provided here.

In general, the manuals listed in Section 3.3.1 are the best starting point and
are not too difficult to understand.

To boot from an OS installation or diagnostic floppy, load the appropriate 
floppy disk image using the "System->Floppy Disk->Load..." menu.  Then select 
the "Floppy" Alternate Boot item from the "System->Alternate Boot" menu and 
start or reset the emulated Star system.  The system should then boot from the 
floppy disk.

When starting the installation of a new operating system from scratch, there 
are a few steps that are not well documented and which are fairly unintuitive:

1) In general it is useful to have the time and date set in the Star's TOD
   clock before booting.  Many Star operating systems and installers 
   *really* want to know what time it is, and they don't trust you to type 
   it in. If the TOD has an invalid time / date it will attempt to get it 
   from the network and in many cases will not proceed until the network 
   responds.  Unless you have an XNS timeserver running on your network 
   (you probably do not), use the Configuration dialog to set the time 
   before booting (See section 4.0).

2) If you are starting with a new unformatted hard disk the installer will
   hang waiting for the disk microcode to read the disk, usually after
   printing the initial banner ("Installer Version X.Y of DD-MMM-YY 
   HH:MM:SS, etc.").  It will sit here indefinitely.

   To get past this, you will need to boot the Diagnostic floppy (usually
   provided with each set of installation floppies) and use the diagnostics
   to format the disk.  This is still more complicated than it should be
   due to the way the disk microcode interacts with an unformatted disk.
   After booting the diagnostic floppy you will be prompted to enter
   timezone and time / date information.

   After entering this information, the diagnostic will print something
   similar to:    
      `XX Megabyte Storage Diagnostic Program 8.0 of 11-Mar-88 11:16:45 PST`    
      `>Fault Analysis`

   And then it will pause for 30-45 seconds and fail with:    
      `Fatal error: Microcode.`
   
   After which the system halts and will not respond to input.

   This is because the Fault Analysis step is expecting a formatted disk
   and your disk is not yet formatted.  The disk microcode is unable to
   cope and goes off into the weeds.

   To work around this, when the `>Fault Analysis` line appears during
   boot, hit the "Stop" key on the Star's keyboard (this is mapped to
   "Page Down.")  The diagnostic will print:    
      `<STOP> key acknowledged`
      Command stopped
    
   And leave you at the ">" prompt.  Now you can format your disk!

   Or can you?  Typing a "?" will give you a list of available commands
   but there's nothing related to disk formatting in that list!

   Xerox didn't want the average person to be able to format disks so this
   functionality is hidden by default.  To enable it, you use the Logon
   command -- Type "Logon" and hit return, and it will ask you for a user 
   name.  Use "Xerox" and then provide the password "wizard" (or "elf", 
   depending on your stature.)  Your privileges will be upgraded and now
   "?" will reveal a host of fun commands!  The "Format" command is what
   you want, and is mostly self-explanatory.  Do *not* save the bad page
   table (as there isn't one, and the microcode will hang trying to read
   it.)  Formatting will take several minutes after which you will be 
   asked if you want to recreate the bad page table (say "yes.").  You 
   will given the option to do a media scan (you can if you want, but 
   emulated disks have no bad spots so there isn't much point.)

   Once the disk has been formatted, you can boot the Installer disk and
   go about doing the actual installation.

3) Yes, it really does take ViewPoint 10-15 minutes to boot the first time.
   It's not particularly swift on subsequent boots, either.  Patience is a
   virtue when using a Star.

4) If you get stuck at MP Code 937 during boot, first try the advice in 
   (1) above.
   Setting dates post-Y2K may cause issues with some operating systems.
   On Viewpoint you might also want to install the Set Time utility
   (see the official Viewpoint docs and installer help for details).

5) The default startup diagnostics that run during Viewpoint or XDE
   boot may fail the RTC test (with flashing MP code 0323 / 0007).
   This occurs if the emulated Star is not running at 100% speed --
   either because Throttling is off (See Section 4.1) or because your
   computer isn't capable of running the emulation at full speed. This is
   because the emulated Star is running faster or slower than the test 
   expects relative to the RTC -- the test thinks that the RTC is behaving
   incorrectly.  In these cases, you can either (1) Enable Throttling
   during boot (if the system is running too fast) or (2) use the 
   "System->Alternate Boot" menu to select "Rigid" rather than "Default" --
   this will bypass startup diagnostics entirely.

The following passwords will allow you to run Viewpoint in perpetuity.  When
using them, ensure the emulated Star's TOD clock is set to a date in
December, 1997 (afterwards the clock can be set to whatever date you like):

    ViewPoint 1.1 / Services 10.0: J SH9R JX2A CH3N
    ViewPoint 2.0 / Services 11.0: 8 7T78 M8YL LFEQ
    

4.0 Configuration
=================

Darkstar's configuration dialog can be invoked with the 
"System->Configuration..." menu.  This is a small window with multiple tabs.
Each tab is explained in detail in the following subsections.


4.1 System Configuration
------------------------

The System Configuration tab provides configuration for three options:

- Memory Size (KW):  Configures the amount of memory installed in the system,
    From 128KW to 768KW in 128KW increments.  This defaults to 768KW.  
    Changes made here will not take effect until the system is reset.

- Host ID (MAC Address):  Configures the Star's Ethernet MAC Address (also used
    as the system's Host ID for licensing.)  If you have multiple instances of
    the emulator running on the same network, all instances should have unique
    MAC addresses, and you'll also want to make sure that no other real devices
    on your network have the same MAC address.  Note that changing this on 
    systems running Viewpoint will likely invalidate any previously entered 
    product factoring (license) keys.

- Throttle Execution Speed:  Checking this box will limit execution speed to 
    the execution speed of a real Star.  When unchecked, the emulation will run
    as fast as the host processor allows.
    Note:  See Section 3.3.3 for potential pitfalls with this option disabled.


4.2 Ethernet Configuration
--------------------------

The Ethernet Configuration tab allows the selection of the host network interface
to use with Darkstar. The listbox for the available interfaces contains at least
the entries `None` (for no ethernet adapter) and `[[ Dodo-Nethub ]]` (for a direct
connection to a [Dodo](https://github.com/devhawala/dodo) NetHub).  If WinPcap or libpcap
is available, further network interfaces will be listed.

If the `[[ Dodo-Nethub ]]` entry is selected, the 2 input fields for the NetHub
Host and Port will be activated for specifying the destination NetHub.


4.3 Display Configuration
-------------------------

The Display Configuration tab provides options for the emulated Star's display:

- Slow Phosphor Simulation:  If checked, the slow phosphor of a real Star's 
    display is simulated.  This is not necessary for any real purpose but looks
    more authentic and incurs no performance penalty.

- Display Scale:  Allows scaling the display by a factor 1, 2, 3 or 4.  This is
    useful on 4k (or higher) resolution displays with a high DPI.

- Stretch screen in Fullscreen mode:  Stretches the Star's display to fill the 
    entire screen in fullscreen mode.  This maintains the original display's
    aspect ratio.  Depending on the resolution of the screen, this may result
    in a blurry display.


4.4 Time Configuration
----------------------

The Time Configuration tab provides options for initializing the Star's TOD
(time of day) clock at the time the emulation is started or reset.  This is
primarily useful to aid in working around the absence of XNS time servers,
the lack of which can cause problems with some Star operating systems.

There are three options for TOD clock initialization:

- Current time/date:  This sets the Star's TOD to the current time/date with
    no adjustments or changes.

- Current time/date with Y2K compensation:  This sets the Star's TOD to the
    current time/date with 28 years subtracted from the date.  This works 
    around software that's not Y2K compliant while still allowing the calendar
    to match up.

- Specified time/date:  This sets the TOD to a specific time and date.  This
    is useful for working around Viewpoint product factoring (license) key
    expiry.

- Specified date:  This sets the TOD to the specified date, using the 
    current (real) time.  This is useful as above, but allows the Star's
    clock to be in sync with reality.

- No change:  This leaves the TOD alone at powerup/reset.  Use this if you
              plan to set the time manually or via XNS, or if you want to 
              maintain the current time / date across resets.


5.0 Debugger
============

Darkstar has an integrated debugger that can make use of microcode and IOP
(8085) source code (if available) to aid in debugging.  The debugger can be
invoked via the "System->Show Debugger" menu.

This debugger is extremely crude, and is not user-friendly at all.

The debugger consists of three windows -- the Console, the CP Debugger,
and the IOP debugger.  Commands can be executed in the Console window, and
sources, disassembly and breakpoints can be viewed in the CP and IOP 
debugger windows.

The "?" or "help" command at the Console window will give you a brief
synopsis of the various commands at your disposal.


6.0 Running from the command line
=================================

The Darkstar program accepts the following optional command line parameters:

- `-config` _configurationFile_    
  specify the configuration file (see below) defining the presets
  for the configuration to be used (these presets can be overriden
  using the configuration dialog)
  
- `-rompath` _path_    
  specify the path where the ROM files are located
  
- `-start`    
  start the emulator when the UI is ready, this requires that the
  disk image to be used is defined (either through the configuration
  file or by the Windows defaults)

(on the Windows platform, these parameters can be given either on the
command line prompt or can be included in the program invocation line
specified for a reference icon)

Using configuration files simplifies using several Star machines, each
consisting of a hard disk image file with the required presettings,
each defined by a specific machine configuration file.

The configuration file is a text file with "`parameter =` _value_" lines
for setting configuration values; empty lines and lines starting with
a hash character are ignored.

The following configuration parameters can be given, matching the
corresponding entries in the configuration dialog or the system menu:

- `MemorySize =` _nnn_        
  system memory size, in KWords as decimal value.

- `HardDriveImage =` _filename_    
  filename for the hard disk image to load

- `FloppyDriveImage =` _filename_    
  filename for the floppy disk image to load
  
- `HostID =` _12-hex-digits_    
  the Ethernet host address for this machine, given as hexadecimal
  number for the 48 bit machine id

- `HostPacketInterfaceName =` _device_     
  the name of the Ethernet adaptor on the emulator host to use for
  Ethernet emulation, one of: `None`, `[[ Dodo-Nethub ]]` or any 
  network adapter listed in the configuration dialog (as recognized
  by WinPcap or libpcap, if present)

- `ThrottleSpeed =` _boolean_    
  whether to cap execution speed at native execution speed or not

- `DisplayScale =` _n_    
  scale factor to apply to the display

- `FullScreenStretch =` _boolean_    
  whether to apply linear or nearest-neighbor filtering to the display,
  when scaled

- `SlowPhosphor =` _boolean_    
  whether to apply a fake "slow phosphor" persistence to the emulated
  display

- `TODSetMode =` _mode_    
  how to set the TOD clock at power up/reset, one of:    
  HostTimeY2K, HostTime, SpecificDateAndTime, SpecificDate, NoChange

- `TODDateTime =` _iso-datetime_    
  the specific date/time to set the TOD clock to if TODSetMode is
  "SpecificDateAndTime"

- `TODDate =` _iso-date_    
  the specific date to set the TOD clock to if TODSetMode is "SpecificDate"

- `AltBootMode =` _mode_    
  the preferred Alt-Boot mode for starting the machine, one of:
  None, DiagnosticRigid, Rigid, Floppy, Ethernet, DiagnosticEthernet,
  DiagnosticFloppy, AlternateEthernet, DiagnosticTrident1, DiagnosticTrident2,
  DiagnosticTrident3, HeadCleaning

- `Start =` _boolean_    
  start the system when the UI is ready? (default: false)


7.0 Known Issues
================

- Speed throttling is not implemented on Unix platforms.
- SDL is forced to software-rendering mode on Unix platformst 
  due to an odd bug that has yet to be solved.  Performance may suffer as a 
  result.


8.0 Reporting Bugs
==================

If you believe you have found a new issue (or have a feature request) please
send an e-mail to joshd@livingcomputers.org or open an issue on the GitHub 
site (see Section 8.0)

When you send a report, please be as specific and detailed as possible:
- What issue are you seeing?
- What software are you running?
- What operating system are you running Darkstar on?
- What are the exact steps needed to reproduce the issue?

The more detailed the bug report, the more possible it is for me to track down
the cause.


9.0 Source Code
===============

The complete source code is available under the BSD license on GitHub at:

https://github.com/livingcomputermuseum/Darkstar

Contributions are welcome!


10.0 Hard Disk Image Format
===========================

The Star's hard drive controller is implemented in microcode and controls the
drive at a very low level, so the hard drive image format is not simply a dump
of the sector data on the disk.

The image consists of a single byte header which indicates the type of SA1000
disk the image contains data for:

    1 - Shugart SA1004 (10MB)
    2 - Quantum Q2040 (40MB)
    3 - Quantum Q2080 (80MB)
        
All other values are currently invalid.  The geometry for the above disks are:

    SA1004 - 256 cylinders, 4 heads (or tracks)
    Q2040  - 512 cylinders, 8 heads
    Q2080  - 1172 cylinders, 7 heads

Following the header are multiple 5325 word blocks, one for each track on the
disk, starting at cylinder 0, head 0, followed by cylinder 0, track 1 and so 
on.  Each word in the disk image is 24 bits wide, written in little-endian 
order:  The most significant 8 bits indicate the type of data in the word, the
low 16 bits are the data word itself:

    0 - Disk data or unused
    1 - Address mark (for header, label, or data)
    2 - CRC

The Star's controller divides each track into 16 sectors; each sector
consists of three fields: Header, Label, and Data.  Each of these begins with
an Address Mark - 0x1a141 for the Header, 0x1a143 for the Label and Data.
Each of the fields end with two words of CRC (currently always a dummy value of
0x2beef).

Xerox specified that the Header is two 16-bit words in length, the Label is
12 words, and the Data field is 256 words.  However: As the writing of address
marks, data, and CRC are controlled by microcode (which could potentially vary
between revisions of the operating system) it is probably best not to make 
assumptions about the positioning and length of the sectors.  If you need to 
extract data, parse each track, looking for the address marks and CRCs to 
delineate the actual data.


11.0 Thanks and Acknowledgements
================================

Darkstar would not have been possible without the amazing preservation work of 
Bitsavers.org

Ethernet encapsulation is provided courtesy of SharpPcap, a WinPcap/LibPcap wrapper.
See: https://github.com/chmorgan/sharppcap.

Display rendering and keyboard/mouse input is provided through SDL 2.0, see:
https://www.libsdl.org/ and is accessed using the SDL2-CS wrapper, see:
https://github.com/flibitijibibo/SDL2-CS.


12.0 Change History
===================

v1.1.9.0
--------
- new network device for direct connection to a Dodo NetHub
- new optional command line parameter "-start" (start system when the UI is up)
- new parameters in configuration-file to match relevant UI items
- fix to Ethernet device for receiving packets larger than 56 bytes
- fix to configuration-dialog to permit 48 significant bits for Host ID
- fix to display for border pattern lines
- added StarOS 5.0 disk image (including configuration file) to Disks subdirectory

v1.1
----
- Floppies can now be formatted and written.
- Tweak to "No change" time configuration option (sets Power Loss flag.)
- Added full screen display mode

v1.0.0.1
--------
- Fixed Ethernet receiver; Ethernet controller now works reliably.
- Cleaned up shutdown code, made hard disk image saving more robust in the face
  of failure
- Removed 1MW memory option since it was never a shipping configuration and
  causes issues with various Xerox software.

v1.0
----
Initial release.
