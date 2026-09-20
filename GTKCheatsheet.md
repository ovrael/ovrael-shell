# GTK 4 — Widget Cheatsheet

## Containers & Layout

| Widget               | Description                                     |
| -------------------- | ----------------------------------------------- |
| `Gtk.Box`            | Arranges widgets horizontally or vertically     |
| `Gtk.CenterBox`      | Arranges widgets at the start, center, and end  |
| `Gtk.Grid`           | Arranges widgets in rows and columns            |
| `Gtk.Overlay`        | Places widgets on top of each other             |
| `Gtk.Stack`          | Displays one child view at a time               |
| `Gtk.StackSwitcher`  | Buttons for switching between `Gtk.Stack` pages |
| `Gtk.StackSidebar`   | Sidebar navigation for a `Gtk.Stack`            |
| `Gtk.Paned`          | Splits an area into two resizable sections      |
| `Gtk.Frame`          | Draws a frame around a child widget             |
| `Gtk.Expander`       | Expandable/collapsible section                  |
| `Gtk.ScrolledWindow` | Provides scrolling for its child                |
| `Gtk.FlowBox`        | Automatically arranges children into rows       |
| `Gtk.Fixed`          | Allows manual positioning of children           |

## Text & Graphics

| Widget            | Description                                         |
| ----------------- | --------------------------------------------------- |
| `Gtk.Label`       | Displays text                                       |
| `Gtk.Inscription` | Displays text with control over its available space |
| `Gtk.Image`       | Displays an icon or image                           |
| `Gtk.Picture`     | Displays a `Gdk.Paintable` or image                 |
| `Gtk.Spinner`     | Displays a loading animation                        |
| `Gtk.ProgressBar` | Displays progress towards a goal                    |
| `Gtk.LevelBar`    | Displays a value within a range                     |

## Buttons

| Widget               | Description                                                  |
| -------------------- | ------------------------------------------------------------ |
| `Gtk.Button`         | Standard clickable button                                    |
| `Gtk.ToggleButton`   | Button with an on/off state                                  |
| `Gtk.CheckButton`    | Check box or radio-style selection                           |
| `Gtk.LinkButton`     | Button that opens a URI                                      |
| `Gtk.MenuButton`     | Button that opens a menu or popover                          |
| `Gtk.VolumeButton`   | Button with a volume control                                 |
| `Gtk.ScaleButton`    | Button with an adjustable scale                              |
| `Gtk.WindowControls` | Window control buttons such as minimize, maximize, and close |

## Input

| Widget              | Description                               |
| ------------------- | ----------------------------------------- |
| `Gtk.Entry`         | Single-line text input                    |
| `Gtk.PasswordEntry` | Single-line password input                |
| `Gtk.SearchEntry`   | Text input designed for searching         |
| `Gtk.Text`          | Single-line editable text                 |
| `Gtk.TextView`      | Multi-line text editor                    |
| `Gtk.EditableLabel` | Label whose text can be edited            |
| `Gtk.SpinButton`    | Input for numeric values                  |
| `Gtk.Scale`         | Slider for selecting a value from a range |
| `Gtk.Switch`        | On/off switch                             |
| `Gtk.DropDown`      | Drop-down selection widget                |

## Lists & Collections

| Widget           | Description                               |
| ---------------- | ----------------------------------------- |
| `Gtk.ListView`   | Efficient scrollable list of items        |
| `Gtk.ColumnView` | List displaying items in multiple columns |
| `Gtk.ListBox`    | Simple list of widgets                    |
| `Gtk.ListBoxRow` | Individual row inside a `Gtk.ListBox`     |
| `Gtk.GridView`   | Efficient grid of items                   |
| `Gtk.FlowBox`    | Automatically arranged grid of widgets    |

## Menus, Popovers & Dialogs

| Widget            | Description                            |
| ----------------- | -------------------------------------- |
| `Gtk.Popover`     | Popup attached to another widget       |
| `Gtk.PopoverMenu` | Menu displayed inside a popover        |
| `Gtk.MenuButton`  | Button that opens a menu               |
| `Gtk.AlertDialog` | Dialog for messages and confirmations  |
| `Gtk.FileDialog`  | Dialog for selecting files and folders |
| `Gtk.ColorDialog` | Dialog for selecting colors            |
| `Gtk.FontDialog`  | Dialog for selecting fonts             |

## Other Useful Widgets

| Widget              | Description                         |
| ------------------- | ----------------------------------- |
| `Gtk.Calendar`      | Calendar widget                     |
| `Gtk.Separator`     | Visual separator between widgets    |
| `Gtk.Revealer`      | Animatedly reveals or hides a child |
| `Gtk.HeaderBar`     | Header bar for application windows  |
| `Gtk.SearchBar`     | Expandable search bar               |
| `Gtk.ShortcutLabel` | Displays a keyboard shortcut        |
| `Gtk.DrawingArea`   | Area for custom drawing             |
| `Gtk.GLArea`        | OpenGL rendering area               |
| `Gtk.Video`         | Video playback widget               |
| `Gtk.MediaControls` | Controls for media playback         |

## Most Commonly Used

| Widget               | Similar Concept               |
| -------------------- | ----------------------------- |
| `Gtk.Box`            | Flexbox                       |
| `Gtk.CenterBox`      | Start / center / end layout   |
| `Gtk.Grid`           | CSS Grid                      |
| `Gtk.Overlay`        | Layers / absolute positioning |
| `Gtk.Label`          | Text                          |
| `Gtk.Image`          | Image / icon                  |
| `Gtk.Button`         | Button                        |
| `Gtk.ToggleButton`   | Toggle                        |
| `Gtk.Entry`          | Input                         |
| `Gtk.Switch`         | Switch                        |
| `Gtk.Scale`          | Slider                        |
| `Gtk.DropDown`       | Select                        |
| `Gtk.ListView`       | List                          |
| `Gtk.Stack`          | Views / tabs                  |
| `Gtk.Popover`        | Popup                         |
| `Gtk.Revealer`       | Animated show/hide            |
| `Gtk.ScrolledWindow` | Scroll container              |
| `Gtk.Separator`      | Divider                       |

## Basic Widget Hierarchy

```text
Gtk.Widget
├── Gtk.Window
│   └── Gtk.ApplicationWindow
│
├── Gtk.Box
├── Gtk.CenterBox
├── Gtk.Grid
├── Gtk.Overlay
├── Gtk.Stack
│
├── Gtk.Label
├── Gtk.Image
├── Gtk.Picture
│
├── Gtk.Button
│   ├── Gtk.ToggleButton
│   ├── Gtk.MenuButton
│   └── ...
│
├── Gtk.Entry
│   ├── Gtk.SearchEntry
│   └── Gtk.PasswordEntry
│
├── Gtk.ListView
├── Gtk.GridView
├── Gtk.ListBox
│
├── Gtk.Popover
├── Gtk.PopoverMenu
│
└── Gtk.ScrolledWindow
```
