# Time Tracker Application

A comprehensive time-tracking application built with a modern technology stack for monitoring employee productivity in remote or multi-user environments.

## 🏗️ Architecture

This is a monorepo containing three main components:

- **Desktop Application** (`/app`) - C++/Qt6 client for Windows
- **Backend API** (`/backend`) - .NET Web API server
- **Frontend Dashboard** (`/frontend`) - Angular web application with Material Design

## 📋 Prerequisites

Before setting up the development environment, ensure you have the following tools installed:

### Required Software

1. **Git** - For version control
2. **Visual Studio 2022** - With "Desktop development with C++" workload
3. **CMake** - Version 3.25 or higher
4. **vcpkg** - C++ package manager (cloned and bootstrapped at `C:/vcpkg`)
5. **Qt6** - Version 6.9.0 or compatible (installed at `C:/Qt/`)
6. **.NET SDK** - Version 8.0 or higher
7. **Node.js** - LTS version (18.x or higher)
8. **Angular CLI** - Latest version

### Installation Links

- [Git](https://git-scm.com/downloads)
- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/)
- [CMake](https://cmake.org/download/)
- [vcpkg](https://github.com/Microsoft/vcpkg)
- [Qt6](https://www.qt.io/download)
- [.NET SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/bramburn/timetracker.git
cd timetracker
```

### 2. Setup vcpkg (One-time setup)

```bash
# Clone vcpkg to C:/vcpkg
git clone https://github.com/Microsoft/vcpkg.git C:/vcpkg
cd C:/vcpkg
./bootstrap-vcpkg.bat
./vcpkg integrate install
```

### 3. Install Qt6

Download and install Qt6 from the official website to `C:/Qt/6.9.0/msvc2022_64/`

### 4. Install Angular CLI

```bash
npm install -g @angular/cli
```

## 🔧 Development Setup

### Desktop Application (C++/Qt6)

```bash
cd app

# Configure the project using CMake presets
cmake --preset vcpkg-qt

# Build the application
cmake --build . --config Debug

# The executable will be in the bin/Debug directory
```

### Backend API (.NET)

```bash
cd backend/TimeTracker.API

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the development server
dotnet run
```

The API will be available at `https://localhost:7000` and `http://localhost:5000`

### Frontend (Angular)

```bash
cd frontend

# Install dependencies
npm install

# Start the development server
ng serve

# Or use npm script
npm start
```

The frontend will be available at `http://localhost:4200`

## 🏃‍♂️ Running the Applications

### Development Mode

1. **Start Backend API:**
   ```bash
   cd backend/TimeTracker.API
   dotnet run
   ```

2. **Start Frontend:**
   ```bash
   cd frontend
   ng serve
   ```

3. **Build and Run Desktop App:**
   ```bash
   cd app
   cmake --build . --config Debug
   ./bin/Debug/TimeTrackerApp.exe
   ```

### Production Build

1. **Backend:**
   ```bash
   cd backend/TimeTracker.API
   dotnet publish -c Release
   ```

2. **Frontend:**
   ```bash
   cd frontend
   ng build --configuration production
   ```

3. **Desktop App:**
   ```bash
   cd app
   cmake --build . --config Release
   ```

## 📁 Project Structure

```
timetracker/
├── app/                    # C++/Qt6 Desktop Application
│   ├── CMakeLists.txt     # CMake build configuration
│   ├── CMakePresets.json  # CMake presets for consistent builds
│   ├── vcpkg.json         # C++ dependencies
│   └── main.cpp           # Application entry point
├── backend/               # .NET Web API
│   └── TimeTracker.API/   # Main API project
│       ├── Program.cs     # API entry point
│       └── *.csproj       # Project file
├── frontend/              # Angular Frontend
│   ├── src/               # Source code
│   ├── package.json       # Node.js dependencies
│   └── angular.json       # Angular configuration
├── docs/                  # Documentation
│   ├── sprint0.md         # Sprint 0 requirements
│   ├── prd0.md           # Product requirements
│   └── prd1.md           # Updated requirements
├── .gitignore            # Git ignore rules
└── README.md             # This file
```

## 🧪 Testing

### Backend Tests
```bash
cd backend/TimeTracker.API
dotnet test
```

### Frontend Tests
```bash
cd frontend
ng test
```

### Desktop App Tests
```bash
cd app
# Tests will be added in future sprints
```

## 🔍 Troubleshooting

### Common Issues

1. **CMake can't find Qt6:**
   - Ensure Qt6 is installed at `C:/Qt/6.9.0/msvc2022_64/`
   - Update the `CMAKE_PREFIX_PATH` in `CMakePresets.json` if needed

2. **vcpkg not found:**
   - Ensure vcpkg is cloned to `C:/vcpkg`
   - Run `vcpkg integrate install`

3. **Node.js version issues:**
   - Use Node.js LTS version (18.x or higher)
   - Consider using nvm for Node.js version management

4. **.NET SDK not found:**
   - Install .NET 8.0 SDK or higher
   - Verify with `dotnet --version`

### Build Verification

To verify all components are working correctly:

1. **Desktop App:** Should show a window with system tray icon
2. **Backend API:** Should respond at the configured endpoints
3. **Frontend:** Should load the Angular application with Material Design

## 📚 Documentation

- [Sprint 0 Requirements](docs/sprint0.md)
- [Sprint 1 Requirements](docs/Sprint1.md)
- [Product Requirements Document](docs/prd0.md)
- [Updated PRD](docs/prd1.md)

## 🤝 Contributing

1. Follow the established project structure
2. Use the provided CMake presets for C++ builds
3. Follow Angular and .NET coding standards
4. Update documentation when adding new features

## 📄 License

This project is proprietary software for internal use.

---

**Sprint 1 Status:** ✅ Complete - Foundational window and system tray functionality implemented
**Sprint 0 Status:** ✅ Complete - All three applications initialized and buildable
