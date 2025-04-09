# ShellCommand Context Menu Extension
(thank you [Oleg](https://github.com/oleg-shilo/shell-x))


![24](https://github.com/user-attachments/assets/84e837d5-685d-4bb9-91e7-8c0d53a60b8a)



This project provides a custom Windows Shell Extension built with .NET 4.8 and SharpShell. It allows you to define multiple context menus depending on the type of item you right-click. The extension supports:

- **File & Folder Context Menus:**
  Use a root filter folder such as `[jpeg,jpg,png]` or `[mp4]` or `[jpeg,jpg,png,mp4]` to define context menus for specific file types.   You can mix them. 
- **Folder Context Menu:**  
  A root folder named `[folder]` to define a menu when right-clicking folders.
- **Background Context Menu:**  
  A root folder named `[background]` to define a menu when right-clicking on the empty space (background) of a folder. Useful for   running scripts on the current folder.

> **Note:** Executed scripts are launched with a visible window.

---
## Installation

1. Download the release.
2. Unzip into the definitive folder.
3. Run "install.bat"

## Uninstallation

1. Run "uninstall.bat"
2. Optionally remove the "ShellCommand" folder inside `%APPDATA%`.

   Warning, doing this will remove all your "configuration" so reinstall will be done from ground up again.

## Customization

The extension supports icons, to use your custom icons make sure they have many sizes embedded in them (mostly 16x16 and 32x32 versions) and name them the same as the folder or script file you have, e.g. Move.bat -> Move.ico. The script is DPI-aware so icons are correctly represented even at %200 scaling (4k). 

To add your own script, first make sure it accepts paths of files or folders as arguments. Then you just put them inside the folder
you want as a context menu e.g. `%APPDATA%\ShellCommand\[jpg,jpeg,png]\Move\myscript.ps1` on this example, you will see a "Move" entry with a submenu with your script ("myscript") only when you right click on a jpg or jpeg, or png file. You're getting it right?😉  
Just keep doing that with all the scripts you need. You can set whathever extension you need.

```
%APPDATA%\ShellCommand\
    [jpeg,jpg,png]\
        <Subfolders or Script Files (e.g., myscript.ps1, etc.)>
    [mp4]\
        <Subfolders or Script Files>
    [folder]\
        <Subfolders or Script Files>
    [background]\
        <Subfolders or Script Files>
```

Hint: if you have python scripts (.py) you can use a bat file as a proxy launcher for the context menu, where you can tell it
to activate your virtual env if needed before launch.




---
## Dev it?

### Prerequisites

- **.NET Framework 4.8** must be installed on your machine.
- **SharpShell** libraries should be referenced in your project.
- Administrator privileges might be required to register or unregister shell extensions.

### Building the Extension

1. **Clone or Download the Project:**  
   Get the source code from this repository.

2. **Open the Solution in Visual Studio:**  
   Open the project in Visual Studio, ensuring that it targets .NET 4.8.

3. **Build the Project:**  
   Build the solution. Ensure that there are no compilation errors.




> [!NOTE]
> Made entirely with Deepseek and ChatGPT.

