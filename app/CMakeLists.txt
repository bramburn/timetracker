# =============================================================================
# Time Tracker Desktop Application - CMakeLists.txt
# =============================================================================

cmake_minimum_required(VERSION 3.25)

project(TimeTrackerApp VERSION 1.0.0)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find required Qt6 components
find_package(Qt6 REQUIRED COMPONENTS Core Gui Widgets)

# Enable Qt MOC, UIC, and RCC
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTOUIC ON)
set(CMAKE_AUTORCC ON)

# Add executable
add_executable(TimeTrackerApp WIN32 main.cpp)

# Link Qt6 libraries
target_link_libraries(TimeTrackerApp PRIVATE 
    Qt6::Core 
    Qt6::Gui 
    Qt6::Widgets
)

# Set output directory
set_target_properties(TimeTrackerApp PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
)

# Copy Qt6 DLLs to output directory on Windows (for development)
if(WIN32)
    add_custom_command(TARGET TimeTrackerApp POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
        $<TARGET_FILE:Qt6::Core>
        $<TARGET_FILE:Qt6::Gui>
        $<TARGET_FILE:Qt6::Widgets>
        $<TARGET_FILE_DIR:TimeTrackerApp>
    )
endif()
