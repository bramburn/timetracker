# TimeTracker Testing Guide

## Overview

This guide provides comprehensive information about testing procedures, best practices, and troubleshooting for the TimeTracker desktop application. The project uses Google Test (GTest) framework for unit testing with Qt6 integration.

## Table of Contents

1. [Test Framework Setup](#test-framework-setup)
2. [Running Tests](#running-tests)
3. [Test Structure](#test-structure)
4. [Writing New Tests](#writing-new-tests)
5. [Coverage Requirements](#coverage-requirements)
6. [Troubleshooting](#troubleshooting)
7. [CI/CD Integration](#cicd-integration)

## Test Framework Setup

### Prerequisites

- **CMake 3.25+**: Build system
- **vcpkg**: Package manager with GTest installed
- **Qt6**: GUI framework (6.9.0 or compatible)
- **Visual Studio 2022**: Compiler with C++17 support

### Installation

1. **Install vcpkg dependencies**:
   ```powershell
   cd app
   vcpkg install --triplet x64-windows
   ```

2. **Configure CMake**:
   ```powershell
   cmake --preset vcpkg-qt
   ```

3. **Build with tests**:
   ```powershell
   cmake --build . --config Debug
   ```

## Running Tests

### Local Testing

#### Run All Tests
```powershell
cd app
ctest --build-config Debug --output-on-failure --verbose
```

#### Run Specific Test Suite
```powershell
# Run only framework sanity tests
ctest -R "FrameworkSanityTest" --verbose

# Run only TimeTrackerMainWindow tests
ctest -R "TimeTrackerMainWindow" --verbose

# Run only screenshot capture tests
ctest -R "ScreenshotCapture" --verbose
```

#### Run Tests with Custom Environment
```powershell
# Run in headless mode (for CI/automated testing)
$env:QT_QPA_PLATFORM="offscreen"
ctest --build-config Debug --output-on-failure
```

### Using CMake Targets

```powershell
# Build and run tests using custom target
cmake --build . --target run_tests --config Debug

# Generate coverage report (if supported)
cmake --build . --target coverage --config Debug
```

## Test Structure

### Test Organization

```
app/tests/
├── main_test.cpp                    # Framework sanity and basic integration tests
├── TimeTrackerMainWindow_test.cpp   # Main window component tests
├── ScreenshotCapture_test.cpp       # Screenshot functionality tests
├── ActivityLogging_test.cpp         # Windows activity logging tests
└── test_utils.h                     # Shared testing utilities and mocks
```

### Test Categories

1. **Framework Sanity Tests**: Verify GTest and Qt integration
2. **Unit Tests**: Test individual components in isolation
3. **Integration Tests**: Test component interactions
4. **System Tests**: Test Windows-specific functionality
5. **Performance Tests**: Verify timing and resource usage

## Writing New Tests

### Basic Test Structure

```cpp
#include <gtest/gtest.h>
#include "test_utils.h"
#include "../YourClass.h"

namespace TimeTrackerTest {

class YourClassTest : public QtTestFixture {
protected:
    void SetUp() override {
        QtTestFixture::SetUp();
        // Additional setup
    }
    
    void TearDown() override {
        // Cleanup
        QtTestFixture::TearDown();
    }
};

TEST_F(YourClassTest, TestMethodName) {
    // Arrange
    YourClass instance;
    
    // Act
    bool result = instance.someMethod();
    
    // Assert
    EXPECT_TRUE(result);
}

} // namespace TimeTrackerTest
```

### Qt-Specific Testing

```cpp
TEST_F(YourClassTest, QtWidgetTest) {
    QWidget widget;
    widget.show();
    
    // Process Qt events
    WidgetTestHelper::processEvents(100);
    
    EXPECT_TRUE(widget.isVisible());
}
```

### Mock Objects

```cpp
TEST_F(YourClassTest, MockFileSystemTest) {
    MockFileSystem mockFs;
    mockFs.setFileExists(false);
    mockFs.setCreateDirectorySuccess(true);
    
    // Use mock in your test
    EXPECT_FALSE(mockFs.fileExists("test.txt"));
}
```

## Coverage Requirements

### Minimum Coverage Targets

- **Core Components**: 70% line coverage
- **Public Methods**: 100% method coverage
- **Error Paths**: All error conditions tested
- **Edge Cases**: Boundary conditions covered

### Generating Coverage Reports

```powershell
# Configure with coverage enabled
cmake --preset vcpkg-qt -DENABLE_COVERAGE=ON

# Build and run tests
cmake --build . --config Debug
ctest --build-config Debug

# Generate coverage report (if tools available)
cmake --build . --target coverage
```

## Troubleshooting

### Common Issues

#### 1. GTest Not Found
```
Error: Could not find GTest
```
**Solution**:
```powershell
vcpkg install gtest --triplet x64-windows
cmake --preset vcpkg-qt  # Reconfigure
```

#### 2. Qt Platform Plugin Missing
```
Error: This application failed to start because no Qt platform plugin could be initialized
```
**Solution**:
```powershell
$env:QT_QPA_PLATFORM="offscreen"  # For headless testing
# Or ensure qwindows.dll is in platforms/ directory
```

#### 3. Windows Hooks Fail in Tests
```
Warning: Failed to set up activity tracking hooks
```
**Solution**: This is expected in test environments. Tests should handle this gracefully.

#### 4. Screenshot Capture Fails
```
Warning: Failed to capture screenshot - grabWindow returned null
```
**Solution**: Use offscreen platform for headless testing:
```powershell
$env:QT_QPA_PLATFORM="offscreen"
```

### Debug Test Failures

#### Verbose Output
```powershell
ctest --build-config Debug --output-on-failure --verbose
```

#### Run Single Test
```powershell
# Run specific test executable directly
./bin/Debug/TimeTrackerTests.exe --gtest_filter="*TestName*"
```

#### Debug with Visual Studio
1. Set TimeTrackerTests as startup project
2. Set command line arguments: `--gtest_filter="*TestName*"`
3. Start debugging (F5)

## CI/CD Integration

### GitHub Actions

The project includes automated testing via GitHub Actions:

- **Triggers**: Push to main/master/develop, Pull Requests
- **Environment**: Windows Server with Qt6 and vcpkg
- **Tests**: All test suites with coverage analysis
- **Artifacts**: Test results and build outputs

### Local CI Simulation

```powershell
# Simulate CI environment locally
$env:QT_QPA_PLATFORM="offscreen"
$env:VCPKG_ROOT="C:/vcpkg"

# Clean build
Remove-Item -Recurse -Force build -ErrorAction SilentlyContinue
cmake --preset vcpkg-qt
cmake --build . --config Debug
ctest --build-config Debug --output-on-failure
```

## Best Practices

### Test Design

1. **Isolation**: Tests should not depend on each other
2. **Deterministic**: Tests should produce consistent results
3. **Fast**: Individual tests should complete quickly
4. **Clear**: Test names should describe what is being tested
5. **Maintainable**: Tests should be easy to update when code changes

### Naming Conventions

- **Test Files**: `ComponentName_test.cpp`
- **Test Classes**: `ComponentNameTest`
- **Test Methods**: `MethodName_Condition_ExpectedResult`

### Example:
```cpp
TEST_F(TimeTrackerMainWindowTest, Constructor_WithValidParent_InitializesCorrectly)
TEST_F(ScreenshotCaptureTest, CaptureScreenshot_InHeadlessMode_HandlesGracefully)
```

## Performance Guidelines

- **Test Suite**: Complete run should finish within 60 seconds
- **Individual Tests**: Should complete within 5 seconds
- **Setup/Teardown**: Minimize expensive operations
- **Resource Usage**: Clean up all allocated resources

## Conclusion

This testing framework provides a solid foundation for maintaining code quality and catching regressions early. Regular testing and adherence to these guidelines will ensure the TimeTracker application remains stable and reliable across different environments and use cases.
