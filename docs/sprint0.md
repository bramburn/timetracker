# Project Backlog: Sprint 0 - Environment and Project Setup

### Introduction

This document provides a detailed backlog for Sprint 0 of the Time
Tracker application. The primary goal of this initial sprint is to
establish a robust, consistent, and scalable development environment for
all components of the project: the C++/Qt6 desktop application, the .NET
Web API backend, and the Angular frontend. This sprint is foundational
and ensures that all developers can start working on subsequent features
with a clean, well-structured, and fully configured monorepo.

### User Stories

- **User Story 1**: Project Setup

  - **Description**: As a developer, I want a complete monorepo
    structure set up with all the necessary projects (frontend, backend,
    desktop app) initialized, so that I can start developing features
    without worrying about configuration.

  - **Actions to Undertake**:

    1.  Initialize a new Git repository.

    2.  Create the top-level directory structure: /app, /backend,
        /frontend.

    3.  Create a root .gitignore file to exclude common temporary files,
        build outputs, and environment-specific files for all three
        projects.

    4.  Initialize the C++/Qt6 project in the /app directory with a
        basic CMakeLists.txt.

    5.  Initialize the .NET Web API project in the /backend directory
        using the dotnet new webapi command.

    6.  Initialize the Angular project in the /frontend directory using
        the ng new command.

    7.  Configure the C++ project environment using vcpkg for dependency
        management and CMakePresets.json for build configurations.

  - **References between Files**:

    - The root .gitignore will contain patterns relevant to all
      subdirectories.

    - The CMakePresets.json in the /app directory will reference the
      vcpkg.cmake toolchain file, which is managed by the vcpkg
      dependency manager.

  - **Acceptance Criteria**:

    1.  A Git repository exists with the specified three-folder
        structure.

    2.  Each of the three projects (app, backend, frontend) can be built
        successfully to produce a \"hello world\" or default template
        application.

    3.  The C++ project (/app) successfully configures and builds using
        the defined CMake presets.

  - **Testing Plan**:

    1.  **Manual Clone & Build**: A developer will clone the repository,
        navigate into each subdirectory (/app, /backend, /frontend), and
        run the respective build commands (cmake \--build, dotnet build,
        ng build).

    2.  **IDE Verification**: Open the root folder in a compatible IDE
        like Visual Studio Code or Visual Studio to ensure that the
        presets and project files are recognized and functional.

### List of Files being Created

- **File 1**: /.gitignore

  - **Purpose**: To specify intentionally untracked files to ignore in
    the entire repository.

  - **Contents**: Will include sections for Visual Studio (\*.suo,
    \*.user), .NET (\[Bb\]in/, \[Oo\]bj/), Angular (node_modules/,
    dist/, .angular/), CMake (build/, CMakeCache.txt), and general OS
    files (.DS_Store, Thumbs.db).

  - **Relationships**: Applies globally to all files and directories
    within the repository.

- **File 2**: /app/CMakeLists.txt

  - **Purpose**: The main build script for the C++/Qt6 desktop
    application, defining project properties, source files, and
    dependencies.

  - **Contents**:

    - cmake_minimum_required(VERSION 3.25)

    - project(TimeTrackerApp VERSION 1.0.0)

    - find_package(Qt6 REQUIRED COMPONENTS Core Gui Widgets)

    - add_executable(TimeTrackerApp WIN32 main.cpp)

    - target_link_libraries(TimeTrackerApp PRIVATE Qt6::Core Qt6::Gui
      Qt6::Widgets)

  - **Relationships**: Is read by CMake to generate the build system.
    Uses find_package to locate the Qt6 libraries specified in
    CMakePresets.json.

- **File 3**: /app/CMakePresets.json

  - **Purpose**: To define standard configure and build settings for the
    C++ application, ensuring a consistent development environment.

  - **Contents**:\
    {\
    \"version\": 3,\
    \"configurePresets\": \[\
    {\
    \"name\": \"vcpkg-qt\",\
    \"displayName\": \"vcpkg + Qt6 (Visual Studio 2022)\",\
    \"description\": \"Configure with vcpkg toolchain and Qt6 for Visual
    Studio 2022\",\
    \"generator\": \"Visual Studio 17 2022\",\
    \"architecture\": \"x64\",\
    \"toolset\": \"host=x64\",\
    \"cacheVariables\": {\
    \"CMAKE_TOOLCHAIN_FILE\":
    \"C:/vcpkg/scripts/buildsystems/vcpkg.cmake\",\
    \"CMAKE_PREFIX_PATH\": \"C:/Qt/6.9.0/msvc2022_64\",\
    \"VCPKG_TARGET_TRIPLET\": \"x64-windows\",\
    \"CMAKE_BUILD_TYPE\": \"Debug\"\
    },\
    \"environment\": {\
    \"VCPKG_ROOT\": \"C:/vcpkg\"\
    }\
    }\
    \],\
    \"buildPresets\": \[\
    {\
    \"name\": \"vcpkg-qt-debug\",\
    \"displayName\": \"Debug Build\",\
    \"configurePreset\": \"vcpkg-qt\",\
    \"configuration\": \"Debug\"\
    },\
    {\
    \"name\": \"vcpkg-qt-release\",\
    \"displayName\": \"Release Build\",\
    \"configurePreset\": \"vcpkg-qt\",\
    \"configuration\": \"Release\"\
    }\
    \]\
    }

  - **Relationships**: Provides configurations used by CMake. It points
    to the vcpkg.cmake toolchain and the Qt6 installation path.

- **File 4**: /app/vcpkg.json

  - **Purpose**: To declare the C++ project\'s dependencies for vcpkg to
    manage.

  - **Contents**:\
    {\
    \"name\": \"time-tracker-app\",\
    \"version-string\": \"1.0.0\",\
    \"dependencies\": \[\
    \"qtbase\"\
    \]\
    }

  - **Relationships**: Read by vcpkg to install the necessary libraries
    (qtbase) when CMake configures the project.

- **File 5**: /backend/TimeTracker.API.csproj

  - **Purpose**: The project file for the .NET Web API, defining its
    properties, dependencies, and build settings.

  - **Contents**: Default file generated by dotnet new webapi, including
    the TargetFramework (e.g., net8.0) and other default settings.

  - **Relationships**: Used by the .NET SDK (dotnet build) to compile
    the backend application.

- **File 6**: /frontend/package.json

  - **Purpose**: To define the Angular project\'s metadata, scripts, and
    dependencies.

  - **Contents**: Default file generated by ng new, including
    dependencies for Angular core, scripts for start, build, test, etc.

  - **Relationships**: Used by npm or yarn to manage packages and run
    scripts.

### Test Cases

- **Test Case 1**: Verify C++ Project Build

  - **Test Data**: The default main.cpp file created by the template,
    which shows a blank window.

  - **Expected Result**: Running cmake \--build \--preset vcpkg-qt-debug
    inside the /app directory completes without errors, and an
    executable is created in the build directory that launches a window.

  - **Testing Tool**: Windows Terminal, CMake, Visual Studio 2022 Build
    Tools.

- **Test Case 2**: Verify .NET Backend Build

  - **Test Data**: The default WeatherForecast controller and service
    created by the template.

  - **Expected Result**: Running dotnet build inside the /backend
    directory completes without errors. Running dotnet run starts a
    local server that responds to requests at the default endpoints.

  - **Testing Tool**: Windows Terminal, .NET 8 SDK.

- **Test Case 3**: Verify Angular Frontend Build

  - **Test Data**: The default application shell created by the Angular
    CLI.

  - **Expected Result**: Running ng build inside the /frontend directory
    completes without errors and creates a dist/ folder with the
    compiled application assets.

  - **Testing Tool**: Windows Terminal, Node.js, Angular CLI.

### Assumptions and Dependencies

- **Assumptions**:

  - The developer has administrative rights on their machine to install
    the necessary tools.

  - The file paths in CMakePresets.json (C:/vcpkg/, C:/Qt/) are the
    correct installation locations on the developer\'s machine.

- **Dependencies**:

  - **Git**: For version control.

  - **Visual Studio 2022**: With the \"Desktop development with C++\"
    workload.

  - **CMake**: Version 3.25 or higher.

  - **vcpkg**: Cloned and bootstrapped at C:/vcpkg.

  - **Qt6**: Version 6.9.0 or compatible, installed at C:/Qt/.

  - **.NET SDK**: Version 8.0 or higher.

  - **Node.js**: LTS version.

  - **Angular CLI**: Latest version.

### Non-Functional Requirements

- **Documentation**: A README.md file will be created in the root
  directory explaining the one-time setup steps for all required
  dependencies.

- **Consistency**: The repository structure must be clean and logical,
  facilitating easy navigation between the different parts of the
  application.

- **Repeatability**: The setup process must be repeatable by any new
  developer joining the project by following the README.md instructions.

### Conclusion

Upon successful completion of Sprint 0, the project will have a solid
foundation. All three application pillars will be established within a
version-controlled monorepo, and the development environment will be
configured and validated. This allows the team to move forward into
Sprint 1 and subsequent feature-development sprints with confidence and
efficiency.
