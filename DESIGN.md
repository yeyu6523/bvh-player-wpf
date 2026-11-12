---
name: BVH Player
description: A precise Windows desktop workspace for inspecting skeletal motion.
colors:
  accent: "#245CD7"
  action-focus: "#3F82F4"
  ink: "#172438"
  secondary-text: "#526278"
  white: "#FFFFFF"
  canvas: "#F2F5F9"
  button-border: "#D7DEE9"
  header-divider: "#DDE4EE"
  separator: "#E5EAF1"
  slider-track: "#C7D2E2"
  thumb-focus: "#152F61"
  viewport: "#111E30"
  viewport-button: "#203147"
  viewport-button-border: "#40516A"
  viewport-button-text: "#E5EDF7"
  viewport-title: "#F1F6FF"
  viewport-state: "#8B9AB0"
  viewport-muted: "#B5C3D8"
  viewport-legend: "#C1CEDF"
  viewport-hint: "#A6B8D0"
  skeleton-left: "#54D8DA"
  skeleton-right: "#FFBE74"
  skeleton-center: "#DEE7F2"
  joint-selected: "#8DFFAE"
  floor: "#162436"
  ground-grid: "#2A3C52"
  axis-x: "#B77B77"
  axis-z: "#6C97AD"
typography:
  title:
    fontFamily: "Microsoft YaHei UI, Segoe UI"
    fontSize: "22px"
    fontWeight: 600
  section-title:
    fontFamily: "Microsoft YaHei UI, Segoe UI"
    fontSize: "14px"
    fontWeight: 600
  body:
    fontFamily: "Microsoft YaHei UI, Segoe UI"
    fontSize: "13px"
    fontWeight: 400
  label:
    fontFamily: "Microsoft YaHei UI, Segoe UI"
    fontSize: "12px"
    fontWeight: 400
  micro:
    fontFamily: "Microsoft YaHei UI, Segoe UI"
    fontSize: "11px"
    fontWeight: 400
  viewport-title:
    fontFamily: "Microsoft YaHei UI, Segoe UI"
    fontSize: "17px"
    fontWeight: 600
  timecode:
    fontFamily: "Consolas"
    fontSize: "13px"
    fontWeight: 400
rounded:
  track: "2px"
  control: "7px"
  panel: "10px"
  viewport: "12px"
spacing:
  related-gap: "6px"
  transport-gap: "7px"
  label-gap: "8px"
  section-gap: "12px"
  panel-gap: "18px"
  workspace-inset: "20px"
  header-inset: "24px"
components:
  button-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.white}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "9px 13px"
  button-neutral:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "9px 13px"
  button-viewport:
    backgroundColor: "{colors.viewport-button}"
    textColor: "{colors.viewport-button-text}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "6px 11px"
  slider:
    backgroundColor: "{colors.slider-track}"
    rounded: "{rounded.track}"
    height: "28px"
  checkbox:
    textColor: "{colors.secondary-text}"
    typography: "{typography.body}"
    padding: "0px 0px 0px 5px"
  hierarchy-tree:
    textColor: "{colors.ink}"
    typography: "{typography.label}"
  tool-panel:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
    rounded: "{rounded.panel}"
    padding: "18px 19px"
    width: "278px"
  playback-panel:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
    rounded: "{rounded.panel}"
    padding: "12px 18px"
  skeleton-viewport:
    backgroundColor: "{colors.viewport}"
    textColor: "{colors.viewport-title}"
    rounded: "{rounded.viewport}"
---

# Design System: BVH Player

## Overview

**Creative North Star: "The Motion Workbench"**

BVH Player is a quiet, precise Windows tool for inspecting skeletal motion. The dark observation field gives the moving skeleton priority; white tool surfaces make file facts, playback choices, and hierarchy easy to scan. The phrase above describes the finished implementation rather than a new visual direction.

The interface uses Chinese task labels, a compact desktop type scale, and familiar WPF controls. Blue identifies commands and adjustable progress. Cyan, warm orange, pale ivory, and mint belong to the skeletal data, where color distinguishes body sides and the inspected joint.

**Key Characteristics:**
- A dark 3D observation field beside light, flat tool surfaces.
- A compact Chinese interface with visible playback controls and keyboard hints.
- One blue UI action color, separated from semantic skeleton colors.
- Fixed desktop structure with a scrollable tool panel at the minimum window size.
- Native WPF interaction, focus, selection, and accessibility conventions.

This is an extraction from `App.xaml`, `MainWindow.xaml`, and `SkeletonView.cs`, checked against `.impeccable/review/desktop.png` and `minimum.png`. The YAML is normative for extracted token values. All YAML `px` lengths represent WPF device-independent pixels (DIP; 1/96 inch), not physical screenshot pixels. WPF font sizes also use DIP. CSS-style padding in the YAML has been converted from WPF `Thickness` ordering. No web implementation or mobile breakpoint is implied.

## Colors

The palette pairs cool, pale tool surfaces with a deep blue-gray observation field; distinct warm and cool skeleton colors make the motion legible.

### Primary

- **Workbench Blue** (`accent`): open-file and play/pause commands, slider progress and thumbs, and the current speed value.
- **Keyboard Blue** (`action-focus`): the stronger border on a keyboard-focused button.
- **Deep Focus Blue** (`thumb-focus`): the slider thumb border when the thumb has keyboard focus.

### Secondary

These are semantic 3D data colors, not additional UI command colors.

- **Left Cyan** (`skeleton-left`): the left body side and its legend dot.
- **Right Amber** (`skeleton-right`): the right body side and its legend dot.
- **Axis Ivory** (`skeleton-center`): central joints and bones, matched by the central legend dot.
- **Inspection Mint** (`joint-selected`): the selected joint sphere only; its connected bones keep their side color.
- **Muted Ground Axes** (`axis-x`, `axis-z`): the short X/Z guides on the floor.

### Neutral

- **Ink / Secondary Text** (`ink`, `secondary-text`): primary labels and supporting metadata on light surfaces.
- **White / Cool Canvas** (`white`, `canvas`): the header and tool panels above the application background.
- **Control and Section Lines** (`button-border`, `header-divider`, `separator`): button outlines, the header edge, and sidebar section dividers.
- **Slider Bed** (`slider-track`): the unfilled part of both speed and timeline tracks.
- **Observation Field** (`viewport`): the 3D viewport backing.
- **View Controls** (`viewport-button`, `viewport-button-border`, `viewport-button-text`): tonal buttons placed over the dark viewport.
- **Viewport Text** (`viewport-title`, `viewport-muted`, `viewport-legend`, `viewport-hint`): descending text emphasis from file title to interaction help.
- **Paused Indicator** (`viewport-state`): the small paused-state dot.
- **Floor / Ground Grid** (`floor`, `ground-grid`): lit ground geometry and its restrained perspective grid.

**The Command and Data Rule.** Use blue for UI commands and progress. Reserve the cyan, amber, ivory, and mint data colors for the skeleton and its legend.

## Typography

**Body and Label Font:** Microsoft YaHei UI with Segoe UI fallback, inherited from the WPF window.

**Timecode Font:** Consolas, used for elapsed and total time.

**Character:** Compact, readable desktop typography. Weight and size establish hierarchy; there is no promotional display typography and no authored tracking or line-height scale. TextBlocks use WPF display text formatting, and the window enables layout rounding.

### Hierarchy

- **Title** (`title`): the application name in the header.
- **Section title** (`section-title`): sidebar group headings.
- **Body** (`body`): standard actions, metadata, and checkbox labels.
- **Label** (`label`): viewport buttons, hierarchy tree, frame count, and supporting labels.
- **Micro** (`micro`): legends, interaction hints, endpoint labels, joint details, and status text.
- **Viewport title** (`viewport-title`): the loaded file name over the dark scene.
- **Timecode** (`timecode`): stable, aligned duration readout beside the timeline.

The selected speed readout uses a local semibold emphasis (15 DIP); the parsing overlay uses a local larger message (19 DIP). These are component-local values, not an expanded general type scale. File names and status text truncate with an ellipsis where the source declares it; the joint detail also exposes the full string through its tooltip.

**The Native Type Rule.** Preserve the Chinese UI font and inherited WPF sizing. Use Consolas only for the timeline timecode.

## Layout

The window starts centered at the default outer size (1320 × 860 DIP) and has a minimum outer size (1080 × 760 DIP). Standard Windows non-client chrome remains part of that outer size; the client area is smaller. There is no compact navigation mode or mobile reflow.

The root grid has four rows: header (78 DIP), flexible workspace, playback region (132 DIP), and status row (32 DIP). The header uses horizontal inset (24 DIP). The workspace uses outer inset (20 DIP) at its left, top, and right; its columns are a flexible viewport, a gap (18 DIP), and a fixed tool panel (278 DIP). The light playback panel sits below the scene with top separation (16 DIP), outer horizontal inset (20 DIP), and internal padding (18 DIP horizontal, 12 DIP vertical).

The sidebar uses internal padding (19 DIP horizontal, 18 DIP vertical). Its outer ScrollViewer has automatic vertical scrolling, disabled horizontal scrolling, and an inner grid minimum height (510 DIP). At the minimum window size the sidebar scrolls vertically: the inspected screenshot is scrolled far enough to keep speed, options, hierarchy, and joint details available, while some file information is above the visible scroll position. These controls are available by scrolling; they are not all simultaneously visible. The viewport and bottom transport remain visible. The nested hierarchy tree retains its own automatic horizontal scroll behavior and minimum height (72 DIP).

The transport reserves an automatic-width button cluster, a spacer (20 DIP), a flexible timeline, and a fixed timecode column (155 DIP). The play/pause button has minimum width (96 DIP). The status line leaves room on the right for keyboard hints, while the left status message may truncate.

**The Stable Workspace Rule.** Keep the tool panel fixed in width and let its content scroll at the minimum window size. Preserve visible playback controls while the scene takes the remaining width and height.

## Elevation & Depth

The UI has no authored drop shadows or elevation effects. White surfaces, thin control outlines, separators, spacing, and the dark/light contrast express grouping. Hover and press states change button surface opacity rather than lifting or animating the control.

The viewport has actual 3D depth. Perspective, a restrained floor grid, an ambient light, two directional lights, and a small specular contribution make the spherical joints and cylindrical bones readable. These scene effects do not establish a shadow vocabulary for the 2D tool panels.

**The Flat Tools Rule.** Keep the 2D controls and panels flat. Use scene lighting for the skeleton's volume.

## Shapes

Buttons have softly rounded corners (`control`), panels a broader corner (`panel`), and the viewport shell the broadest corner (`viewport`). Slider tracks use the small `track` radius. The header's compact blue mark has a local corner radius (9 DIP) inside its square footprint (38 × 38 DIP).

Buttons use a thin outline (1 DIP); keyboard focus increases it (2 DIP). Section dividers are hairlines (1 DIP). The hierarchy tree is borderless. Viewport content clips to its rectangular control bounds; the source's rounded Border is not an explicit rounded geometry mask, so the scene can read square at its lower edges in the captures.

The skeleton uses shared sphere and cylinder meshes. Both meshes are frozen, and the shared material groups are frozen; playback updates per-joint and per-bone transforms. Preserve that rendering convention instead of rebuilding geometry each frame.

## Components

### Buttons

Compact, direct commands with clear weight.

- **Neutral:** `button-neutral`, used for the demo action, stepping, stop, and speed presets.
- **Primary:** `button-primary`, used for open and play/pause. The main play control includes a vector icon and text.
- **Viewport:** `button-viewport`, a dark tonal variant for front, side, and reset camera commands.
- **Size:** standard minimum height (36 DIP); viewport minimum height (32 DIP). Speed presets locally use vertical padding (5 DIP).
- **States:** hovered surface opacity (0.8), pressed surface opacity (0.6), disabled surface opacity (0.4). Keyboard focus uses the focus-blue border and thickness (2 DIP). These are immediate WPF triggers with no authored transition.
- **Icons:** simple WPF Path geometry. Keep icon-only transport controls paired with their AutomationProperties names and keyboard-bearing tooltips.

### Sliders

Blue progress over a quiet blue-gray bed.

- The same custom WPF Slider template serves speed and timeline.
- Control height (28 DIP), track thickness (4 DIP), and inner horizontal margin (7 DIP).
- Thumb footprint (14 × 18 DIP), white border (2 DIP), rounded silhouette (7 DIP), and deep-blue border on keyboard focus.
- Click-to-position is enabled. Speed spans 0.1×–3.0× with snapping at 0.1× increments; its four presets are 0.5×, 1×, 1.5×, and 2×.
- The timeline uses frame values, exposes an automation name, and supports seeking while paused or playing.

### Checkboxes and Hierarchy Tree

Familiar Windows task controls.

- Checkboxes retain the platform template, with secondary-text labels, left content padding (5 DIP), and bottom spacing (7 DIP). Preserve native toggle, keyboard, and focus behavior.
- The TreeView retains the WPF/platform expanders, indentation, active and inactive selection, scrolling, and keyboard navigation. Do not substitute fixed brand colors for system selection brushes.
- Tree labels use the compact label role. Selecting a node changes the joint sphere to mint and shows the declared channel order beneath the tree.
- Long channel information truncates visually and remains available in a tooltip.

### Tool and Playback Panels

Flat white work surfaces.

- The sidebar uses `tool-panel`; the transport uses `playback-panel`.
- The sidebar groups motion facts, speed and options, and the skeleton hierarchy with thin separators.
- Values in the facts grid align right, while labels align left. Keep that scan pattern when adding equivalent metadata.
- The sidebar's outer scrolling behavior is part of this component, not a temporary screenshot accommodation.

### Skeleton Viewport

A quiet observation field with data-led motion.

- The loaded file and play state sit at the top left; three camera commands sit at the top right. Side-color legend and mouse help sit at the lower left.
- A PerspectiveCamera uses field of view (40°). Left drag orbits; wheel zooms; right or middle drag pans. Front, side, and reset buttons provide repeatable views.
- Normalized standing height is (2.8 scene units). Standard joints use radius (0.039), the root uses (0.057), end sites use (0.024), and bones use radius (0.021); these are world-space values rather than DIP.
- A loading overlay uses WPF ARGB (`#D9111E30`) and a centered message. This one-off translucent state is not a separate surface theme.
- Shared frozen meshes and materials keep the scene visually stable while transforms change during playback.

### Status and Keyboard Help

Persistent, low-emphasis operating guidance.

- Keep help in the micro type role below the transport.
- Shortcuts are Space for play/pause, Left/Right for frames, R for reset, Home for stop/first frame, and Ctrl+O for open. The footer shows the most frequently used subset; tooltips provide the individual control shortcut.
- The status message reports the current file or operation without competing with the scene.

## Do's and Don'ts

### Do:
- **Do** preserve the light tool panels and dark viewport as separate visual roles.
- **Do** use the single blue action color for commands and progress, and use skeleton colors only for their data meaning.
- **Do** treat all interface measurements as WPF DIP and preserve native text scaling and layout rounding.
- **Do** keep the bottom playback controls visible and allow the fixed-width sidebar to scroll at the minimum window size.
- **Do** preserve keyboard focus, native checkbox and tree behavior, vector icons, tooltips, and automation names.
- **Do** reuse and freeze shared 3D meshes and materials; animate transforms instead of rebuilding geometry.

### Don't:
- **Don't** add decorative shadows or hover lift to the flat tool surfaces.
- **Don't** introduce a mobile breakpoint or collapse the sidebar based on these desktop captures.
- **Don't** replace WPF system tree selection or checkbox styling with invented fixed states.
- **Don't** describe the sidebar as fully visible without scrolling at the minimum size.
- **Don't** promote world-space skeleton radii or screenshot pixels into interface DIP tokens.
- **Don't** treat the sidecar's HTML/CSS previews as application source or a replacement for native WPF controls.

