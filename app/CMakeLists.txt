# =============================================================================
# Time Tracker Desktop Application - CMakeLists.txt
# =============================================================================

cmake_minimum_required(VERSION 3.25)

project(TimeTrackerApp VERSION 1.0.0)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Set Qt6 path explicitly
set(CMAKE_PREFIX_PATH "C:/Qt/6.9.0/msvc2022_64")

# Find required Qt6 components
find_package(Qt6 REQUIRED COMPONENTS Core Gui Widgets)

# Enable Qt MOC, UIC, and RCC
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTOUIC ON)
set(CMAKE_AUTORCC ON)

# Enable testing
enable_testing()

# Find GTest package (optional for now)
find_package(GTest CONFIG QUIET)

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
    # Add test executable
    add_executable(TimeTrackerTests
        tests/main_test.cpp
    )

    # Link the library and GTest to the test executable
    target_link_libraries(TimeTrackerTests PRIVATE
        TimeTrackerLib
        GTest::gtest
        GTest::gtest_main
    )

    # Set output directory for tests
    set_target_properties(TimeTrackerTests PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
    )

    # Discover and register tests with CTest
    include(GoogleTest)
    gtest_discover_tests(TimeTrackerTests)

    message(STATUS "GTest found - building tests")
else()
    message(STATUS "GTest not found - skipping tests")
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
