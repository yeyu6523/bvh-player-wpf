# BVH Player

<!-- impeccable:product-schema 1 -->

## Platform

Windows desktop application.

## Users

Students demonstrating BVH motion-capture parsing for a programming assignment.

## Product Purpose

Open a BVH file, reconstruct its skeletal hierarchy and motion, and control playback with play, pause, and adjustable speed.

## Stack

Implementation choice under the user's permission to use another programming environment: C# / WPF, .NET 10, Windows x64. No Unity installation is required. This is a tool application with code-first implementation.

## Operating Context

The user supplied a Chinese BVH teaching presentation and eleven local sample files. The requested deliverables are an unpacked source repository URL and a compiled executable on the desktop.

## Capabilities and Constraints

Parse HIERARCHY, OFFSET, CHANNELS, End Site and MOTION. Honor declared channel order. Support play/pause, speed, seek, frame stepping, loop and camera orbit/zoom. Package a self-contained Windows executable. Do not publish the teaching presentation or original sample files; include an independently generated demo instead.

## Evidence on Hand

Course presentation: D:/desktop/Downloads/BVH解析/BVH文件解析.ppt. Test inputs: D:/desktop/Downloads/BVH解析/data. GitHub CLI is authenticated. There was no existing application in the workspace.

## Product Principles

- The skeleton and actual motion are the main content.
- Playback controls must be visible immediately and work with keyboard input.
- Report malformed inputs without losing the current loaded animation.
- Source and build commands must be sufficient to reproduce the program.

## Defaults

Professional implementation defaults chosen for this assignment: Chinese interface, English code identifiers, 0.1–3.0 playback speed, start paused, dark 3D viewport with light tool panels. These are implementation assumptions, not additional requirements from the user.
