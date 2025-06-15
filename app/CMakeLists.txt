# =============================================================================
# Time Tracker Desktop Application - CMakeLists.txt
# =============================================================================

cmake_minimum_required(VERSION 3.25)

# =============================================================================
# vcpkg Toolchain Integration
# =============================================================================

# Set vcpkg toolchain file if not already set and vcpkg is available
if(NOT DEFINED CMAKE_TOOLCHAIN_FILE)
    if(DEFINED ENV{VCPKG_ROOT})
        set(CMAKE_TOOLCHAIN_FILE "$ENV{VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake"
            CACHE STRING "vcpkg toolchain file")
        message(STATUS "Using vcpkg toolchain from environment: ${CMAKE_TOOLCHAIN_FILE}")
    elseif(EXISTS "C:/vcpkg/scripts/buildsystems/vcpkg.cmake")
        set(CMAKE_TOOLCHAIN_FILE "C:/vcpkg/scripts/buildsystems/vcpkg.cmake"
            CACHE STRING "vcpkg toolchain file")
        message(STATUS "Using vcpkg toolchain from default location: ${CMAKE_TOOLCHAIN_FILE}")
    else()
        message(WARNING "vcpkg toolchain not found. Please set VCPKG_ROOT environment variable or install vcpkg to C:/vcpkg")
    endif()
endif()

# Set vcpkg target triplet if not already set
if(NOT DEFINED VCPKG_TARGET_TRIPLET)
    if(WIN32)
        set(VCPKG_TARGET_TRIPLET "x64-windows" CACHE STRING "vcpkg target triplet")
    endif()
endif()

project(TimeTrackerApp VERSION 1.0.0)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# =============================================================================
# Build Configuration Options
# =============================================================================

# Option to enable code coverage
option(ENABLE_COVERAGE "Enable code coverage reporting" OFF)

# Configure coverage flags if enabled
if(ENABLE_COVERAGE AND CMAKE_CXX_COMPILER_ID MATCHES "GNU|Clang")
    message(STATUS "Code coverage enabled")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} --coverage -fprofile-arcs -ftest-coverage")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} --coverage")
endif()

# Set Qt6 path explicitly
set(CMAKE_PREFIX_PATH "C:/Qt/6.9.0/msvc2022_64")

# Find required Qt6 components
find_package(Qt6 REQUIRED COMPONENTS Core Gui Widgets Test)

# Enable Qt MOC, UIC, and RCC
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTOUIC ON)
set(CMAKE_AUTORCC ON)

# Enable testing
enable_testing()

# =============================================================================
# Testing Framework Configuration
# =============================================================================

# Find GTest package with improved error handling
find_package(GTest CONFIG QUIET)

if(GTest_FOUND)
    message(STATUS "GTest found via CONFIG mode: ${GTest_VERSION}")
    message(STATUS "GTest include dirs: ${GTest_INCLUDE_DIRS}")
    message(STATUS "GTest libraries: ${GTest_LIBRARIES}")
else()
    message(STATUS "GTest not found via CONFIG mode, trying MODULE mode...")
    find_package(GTest MODULE QUIET)

    if(GTest_FOUND)
        message(STATUS "GTest found via MODULE mode")
    else()
        # Try to find GTest manually in vcpkg
        find_path(GTEST_INCLUDE_DIR gtest/gtest.h
            PATHS ${VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}/include
            NO_DEFAULT_PATH
        )

        if(GTEST_INCLUDE_DIR)
            message(STATUS "GTest headers found in vcpkg: ${GTEST_INCLUDE_DIR}")

            # Find GTest libraries manually
            find_library(GTEST_LIBRARY gtest
                PATHS ${VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}/lib
                NO_DEFAULT_PATH
            )
            find_library(GTEST_MAIN_LIBRARY gtest_main
                PATHS ${VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}/lib
                NO_DEFAULT_PATH
            )

            if(GTEST_LIBRARY AND GTEST_MAIN_LIBRARY)
                # Create imported targets manually
                add_library(GTest::gtest UNKNOWN IMPORTED)
                set_target_properties(GTest::gtest PROPERTIES
                    IMPORTED_LOCATION "${GTEST_LIBRARY}"
                    INTERFACE_INCLUDE_DIRECTORIES "${GTEST_INCLUDE_DIR}"
                )

                add_library(GTest::gtest_main UNKNOWN IMPORTED)
                set_target_properties(GTest::gtest_main PROPERTIES
                    IMPORTED_LOCATION "${GTEST_MAIN_LIBRARY}"
                    INTERFACE_INCLUDE_DIRECTORIES "${GTEST_INCLUDE_DIR}"
                )

                set(GTest_FOUND TRUE)
                message(STATUS "GTest libraries found and imported targets created")
            else()
                message(WARNING "GTest libraries not found. Tests will be disabled.")
                set(GTest_FOUND FALSE)
            endif()
        else()
            message(WARNING "GTest not found. Tests will be disabled.")
            message(STATUS "To enable tests, install GTest via vcpkg: vcpkg install gtest")
        endif()
    endif()
endif()

# =============================================================================
# TimeTrackerLib - Static Library
# =============================================================================

# Create static library with core application logic
add_library(TimeTrackerLib STATIC
    TimeTrackerMainWindow.h
    TimeTrackerMainWindow.cpp
)

# Link Qt6 libraries to the library
target_link_libraries(TimeTrackerLib PUBLIC
    Qt6::Core
    Qt6::Gui
    Qt6::Widgets
)

# Link Windows libraries for API hooks and screenshot capture (Windows-specific)
if(WIN32)
    target_link_libraries(TimeTrackerLib PUBLIC
        User32.lib
        Gdi32.lib
    )
endif()

# Set include directories for the library
target_include_directories(TimeTrackerLib PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

# =============================================================================
# TimeTrackerApp - Main Executable
# =============================================================================

# Add main executable
add_executable(TimeTrackerApp WIN32 main.cpp)

# Link the library to the main executable
target_link_libraries(TimeTrackerApp PRIVATE TimeTrackerLib)

# Set output directory
set_target_properties(TimeTrackerApp PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
)

# =============================================================================
# TimeTrackerTests - Test Executable (only if GTest is found)
# =============================================================================

if(GTest_FOUND)
    # =============================================================================
    # Test Executable Configuration
    # =============================================================================

    # Collect all test source files
    file(GLOB_RECURSE TEST_SOURCES "tests/*.cpp")

    # Add test executable
    add_executable(TimeTrackerTests ${TEST_SOURCES})

    # Set C++ standard for tests (same as main project)
    target_compile_features(TimeTrackerTests PRIVATE cxx_std_17)

    # Link the library and GTest to the test executable
    target_link_libraries(TimeTrackerTests PRIVATE
        TimeTrackerLib
        Qt6::Test
        GTest::gtest
        GTest::gtest_main
    )

    # Set output directory for tests
    set_target_properties(TimeTrackerTests PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
        CXX_STANDARD 17
        CXX_STANDARD_REQUIRED ON
    )

    # Add include directories for tests
    target_include_directories(TimeTrackerTests PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/tests
    )

    # =============================================================================
    # CTest Integration and Configuration
    # =============================================================================

    # Include GoogleTest module for test discovery
    include(GoogleTest)

    # Configure test discovery with enhanced options
    gtest_discover_tests(TimeTrackerTests
        WORKING_DIRECTORY ${CMAKE_BINARY_DIR}/bin
        TIMEOUT 300
        DISCOVERY_TIMEOUT 60
        PROPERTIES
            TIMEOUT 60
            ENVIRONMENT "QT_QPA_PLATFORM=offscreen"
    )

    # Add custom test target for convenience
    add_custom_target(run_tests
        COMMAND ${CMAKE_CTEST_COMMAND} --output-on-failure --verbose
        DEPENDS TimeTrackerTests
        WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
        COMMENT "Running all tests with verbose output"
    )

    # Add test coverage target (if supported)
    if(CMAKE_CXX_COMPILER_ID MATCHES "GNU|Clang")
        add_custom_target(coverage
            COMMAND ${CMAKE_CTEST_COMMAND} --output-on-failure
            COMMAND gcov ${CMAKE_BINARY_DIR}/CMakeFiles/TimeTrackerTests.dir/tests/*.gcno
            DEPENDS TimeTrackerTests
            WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
            COMMENT "Generating test coverage report"
        )
    endif()

    message(STATUS "GTest found - building tests with enhanced configuration")
    message(STATUS "Test sources: ${TEST_SOURCES}")
else()
    message(STATUS "GTest not found - skipping tests")
    message(STATUS "To enable tests, install GTest via vcpkg: vcpkg install gtest")
endif()

# =============================================================================
# Windows-specific configurations
# =============================================================================

# Copy Qt6 DLLs to output directory on Windows (for development)
if(WIN32)
    # Copy Qt DLLs for main app
    add_custom_command(TARGET TimeTrackerApp POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
        $<TARGET_FILE:Qt6::Core>
        $<TARGET_FILE:Qt6::Gui>
        $<TARGET_FILE:Qt6::Widgets>
        $<TARGET_FILE_DIR:TimeTrackerApp>
    )

    # Copy Qt DLLs for tests (only if tests are built)
    if(TARGET TimeTrackerTests)
        add_custom_command(TARGET TimeTrackerTests POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            $<TARGET_FILE:Qt6::Core>
            $<TARGET_FILE:Qt6::Gui>
            $<TARGET_FILE:Qt6::Widgets>
            $<TARGET_FILE_DIR:TimeTrackerTests>
        )

        # Copy Qt platform plugins for tests - improved with error handling
        add_custom_command(TARGET TimeTrackerTests POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E make_directory
            $<TARGET_FILE_DIR:TimeTrackerTests>/platforms
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_PREFIX_PATH}/plugins/platforms/qwindows.dll"
            $<TARGET_FILE_DIR:TimeTrackerTests>/platforms/
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_PREFIX_PATH}/plugins/platforms/qminimal.dll"
            $<TARGET_FILE_DIR:TimeTrackerTests>/platforms/
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_PREFIX_PATH}/plugins/platforms/qoffscreen.dll"
            $<TARGET_FILE_DIR:TimeTrackerTests>/platforms/
            COMMENT "Copying Qt platform plugins for tests"
        )
    endif()

    # Use windeployqt for proper Qt deployment (most reliable method)
    find_program(WINDEPLOYQT_EXECUTABLE windeployqt HINTS ${CMAKE_PREFIX_PATH}/bin)

    if(WINDEPLOYQT_EXECUTABLE)
        add_custom_command(TARGET TimeTrackerApp POST_BUILD
            COMMAND ${WINDEPLOYQT_EXECUTABLE} --debug --compiler-runtime $<TARGET_FILE:TimeTrackerApp>
            COMMENT "Deploying Qt libraries and plugins using windeployqt"
        )
        message(STATUS "Found windeployqt: ${WINDEPLOYQT_EXECUTABLE}")
    else()
        message(WARNING "windeployqt not found, falling back to manual plugin copying")

        # Fallback: Copy Qt platform plugins manually
        add_custom_command(TARGET TimeTrackerApp POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E make_directory
            $<TARGET_FILE_DIR:TimeTrackerApp>/platforms
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_PREFIX_PATH}/plugins/platforms/qwindows.dll"
            $<TARGET_FILE_DIR:TimeTrackerApp>/platforms/
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_PREFIX_PATH}/plugins/platforms/qminimal.dll"
            $<TARGET_FILE_DIR:TimeTrackerApp>/platforms/
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_PREFIX_PATH}/plugins/platforms/qoffscreen.dll"
            $<TARGET_FILE_DIR:TimeTrackerApp>/platforms/
            COMMENT "Copying Qt platform plugins manually"
        )

        # Copy additional Qt plugins that might be needed
        add_custom_command(TARGET TimeTrackerApp POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E make_directory
            $<TARGET_FILE_DIR:TimeTrackerApp>/imageformats
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_PREFIX_PATH}/plugins/imageformats/qico.dll"
            $<TARGET_FILE_DIR:TimeTrackerApp>/imageformats/
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_PREFIX_PATH}/plugins/imageformats/qjpeg.dll"
            $<TARGET_FILE_DIR:TimeTrackerApp>/imageformats/
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${CMAKE_PREFIX_PATH}/plugins/imageformats/qpng.dll"
            $<TARGET_FILE_DIR:TimeTrackerApp>/imageformats/
            COMMENT "Copying Qt image format plugins"
        )
    endif()
endif()
