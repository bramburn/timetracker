# **Project Backlog: Sprint 8 \- Idle Time Detection and Annotation (TDD)**

**Date:** June 15, 2025 **Version:** 1.2

## **1\. Introduction**

This document provides a detailed backlog for Sprint 8 of the Time Tracker application project. This sprint introduces a key business logic feature: the ability to detect when a user is inactive and allow them to provide context for that idle time. This functionality, outlined as feature F-09 in the Product Requirements Document (PRD), is crucial for providing an accurate picture of employee productivity.

For this sprint, the team will adopt a **Test-Driven Development (TDD)** methodology to ensure high code quality, reliability, and maintainability.

## **2\. User Stories**

### **User Story 1: Account for Periods of Inactivity**

* **ID**: US-004  
* **Title**: As an Employee, I want the application to detect when I've been away from my computer and ask me what I was doing, so I can accurately account for my time spent on offline tasks like meetings or breaks.  
* **Description**: When no keyboard or mouse activity is detected for a configurable duration (e.g., 5 minutes), the application should start an "idle timer." When activity resumes, the application should present a simple dialog asking the user how they would like to categorize the idle period (e.g., "Keep as idle," "Meeting," "Break," "Offline Work") and optionally add a brief note for more detail. This annotated data must be sent to the server for accurate reporting. Managers will use this data to understand the complete workday, not just screen time.  
* **Priority**: Medium

## **3\. Actions to Undertake (TDD Workflow)**

Work will follow the Red-Green-Refactor cycle. Each feature will begin with a failing test, followed by the minimal code to make it pass, and then a cleanup phase.

### **3.1. Backend (.NET Web API) Actions**

1. **Test the Database Model**:  
   * **Red**: Write a failing unit test to validate the `IdleSession` model. The test should assert that a `Note` can be assigned and is nullable.  
   * **Green**: Create the `IdleSession.cs` entity model with `StartTime`, `EndTime`, `Reason`, `Note` (nullable string), and `UserId` properties to pass the test.  
   * **Refactor**: Clean up the model and test code.  
2. **Test the API Endpoint**:  
   * **Red**: Write a failing integration test for the `POST /api/idletime` endpoint using an in-memory database. The test will simulate a POST request with idle time data and assert that the endpoint returns a 200 OK status.  
   * **Green**: Create the `TrackingDataController` and the `POST /api/idletime` method stub. Write the minimal code to accept the data and save it to the database to make the test pass.  
   * **Refactor**: Improve the controller logic and error handling.  
3. **Implement Database Migration**:  
   * With the model and controller validated by tests, generate a new database migration using Entity Framework Core to add the `IdleSessions` table with the `Note` column. Apply the migration.

### **3.2. Client (C++/Qt) Actions**

1. **Test Idle Detection Logic**:  
   * **Red**: Using the `QTest` framework, write a failing unit test for a new `IdleDetector` class. The test will simulate a lack of activity signals and assert that the detector correctly enters an "idle state."  
   * **Green**: Implement the minimal logic in the `IdleDetector` class (using a `QTimer`) to detect the idle state and pass the test.  
   * **Refactor**: Integrate the `IdleDetector` into `TimeTrackerMainWindow` and refine the implementation.  
2. **Test Idle Annotation UI**:  
   * **Red**: Write a failing test for the new `IdleAnnotationDialog`. The test should create an instance of the dialog, programmatically set input in the dropdown and note field, simulate a button click, and assert that a signal is emitted with the correct data.  
   * **Green**: Implement the `IdleAnnotationDialog` UI and its signal/slot logic to make the test pass.  
   * **Refactor**: Clean up the dialog's code.  
3. **Test the API Service**:  
   * **Red**: Write a failing unit test for the `ApiService`. This test will mock the `QNetworkAccessManager` and verify that calling `uploadIdleTime` results in a network request being created with the correct URL, HTTP method, and a JSON body containing the reason and note.  
   * **Green**: Implement the `uploadIdleTime` method in `ApiService` to construct and send the `QNetworkRequest`, making the test pass.  
   * **Refactor**: Improve error handling and data serialization in the `ApiService`.

## **4\. References between Files**

* **`client/tests/`**: Will contain new test files (`test_idle_detector.cpp`, `test_idle_annotation_dialog.cpp`) that drive the implementation of client-side features.  
* **`backend/tests/`**: Will contain new test files (`IdleTimeControllerTests.cs`) that drive the implementation of the backend API.  
* All other file references remain as previously described, but their creation and modification will be guided by the test cases.

## **5\. List of Files being Created/Modified**

### **5.1. Client (New/Modified Files)**

* **File 1**: `app/IdleDetector.h`, `app/IdleDetector.cpp` (New, Test-Driven)  
  * **Purpose**: To contain the isolated logic for detecting user inactivity.  
* **File 2**: `app/IdleAnnotationDialog.h`, `app/IdleAnnotationDialog.cpp` (New, Test-Driven)  
  * **Purpose**: A new `QDialog` class for the user to annotate their idle time.  
* **File 3**: `app/ApiService.h`, `app/ApiService.cpp` (Modified, Test-Driven)  
  * **Purpose**: To add the `uploadIdleTime` method.  
* **File 4**: `app/tests/` (New/Modified)  
  * **Purpose**: To house the new QTest unit tests that drive the development of the above features.

### **5.2. Backend (New/Modified Files)**

* **File 5**: `backend/TimeTracker.API/Models/IdleSession.cs` (New, Test-Driven)  
  * **Purpose**: To define the data structure for an idle time period.  
* **File 6**: `backend/TimeTracker.API/Controllers/TrackingDataController.cs` (Modified, Test-Driven)  
  * **Purpose**: To add the new endpoint for receiving idle time data.  
* **File 7**: `backend/TimeTracker.API.Tests/` (New/Modified)  
  * **Purpose**: To house the new xUnit tests that drive the development of the API.  
* **File 8**: `backend/TimeTracker.API/Data/TimeTrackerDbContext.cs` (Modified)  
  * **Purpose**: To register the new `IdleSession` entity.

## **6\. Acceptance Criteria**

(These remain the same as they define the final state of the feature.)

1. If no mouse or keyboard activity is detected for a pre-defined duration (e.g., 5 minutes), the application must enter an "idle" state.  
2. When the user resumes activity, a dialog box must appear, informing them of the duration of their inactivity.  
3. The dialog box must provide the user with options to categorize the idle time and an optional text field to add a note.  
4. Submitting the dialog must trigger an API call to a `POST /api/idletime` endpoint with the start time, end time, selected reason, and the optional note.  
5. The backend must successfully receive this data and store it in a new `IdleSessions` table in the PostgreSQL database, including the note if provided.  
6. The idle detection logic must not interfere with the existing screenshot and activity tracking features.

## **7\. Test-Driven Development Plan**

### **7.1. TDD Workflow: Red-Green-Refactor**

All new functionality will be built by following this cycle:

1. **Red**: Write a small, automated test case for a new feature. The test must fail because the feature doesn't exist yet.  
2. **Green**: Write the simplest possible production code to make the failing test pass.  
3. **Refactor**: Clean up and improve the design of both the production and test code, ensuring all tests continue to pass.

### **7.2. Unit & Integration Tests (The "Red" Step)**

* **Test Case 1 (Backend)**: Create a failing test for the `POST /api/idletime` endpoint.  
  * **Description**: Verify the backend endpoint correctly processes and stores idle session data including a note.  
  * **Test Data**: A sample JSON object representing an idle session with a populated `note` field.  
  * **Expected Result**: The endpoint returns a 200 OK status. A new record appears in the `IdleSessions` table with the correct data, including the text from the note.  
  * **Testing Tool**: xUnit, Moq, EF Core In-Memory Provider.  
* **Test Case 2 (Client)**: Create a failing test for the `IdleDetector`.  
  * **Description**: Ensure the idle state is triggered correctly.  
  * **Test Steps**: 1\. Set the idle threshold to a short duration (e.g., 100ms). 2\. Create the detector. 3\. Do not send any activity signals for the duration. 4\. Assert that the detector's `isIdle()` method returns true.  
  * **Expected Result**: The test fails until the timer-based detection logic is implemented.  
  * **Testing Tool**: Qt Test Framework.  
* **Test Case 3 (Client)**: Create a failing test for `IdleAnnotationDialog`.  
  * **Description**: Test that the dialog emits the correct data.  
  * **Test Steps**: 1\. Create a `QSignalSpy` to monitor the dialog's `submitted` signal. 2\. Instantiate the dialog. 3\. Set the text of the note field. 4\. Simulate a click on the submit button. 5\. Assert that the signal spy has received one signal containing the expected data.  
  * **Expected Result**: The test fails until the UI and signal logic are implemented.  
  * **Testing Tool**: Qt Test Framework, `QSignalSpy`.

### **7.3. Final Acceptance Testing**

After the TDD cycles are complete and all unit/integration tests are passing, the following end-to-end tests will be performed manually.

* **Test Case 4**: End-to-End Idle Time Flow with Note  
  * **Description**: Test the full cycle from idle detection to data storage.  
  * **Test Steps**: 1\. Run the full client and backend stack. 2\. Trigger an idle period on the client. 3\. Resume activity and submit an annotation with a note. 4\. Check the PostgreSQL database.  
  * **Expected Result**: A new, correctly annotated record exists in the `IdleSessions` table, and the `note` column contains the text entered by the user.  
  * **Testing Tool**: Running application, PostgreSQL client.

## **8\. Assumptions and Dependencies**

* **Assumption 1**: The definition of "idle" (e.g., 5 minutes of no input) is acceptable for the initial implementation.  
* **Dependency 1**: This sprint relies on the successful implementation of the Windows API hooks from Sprint 3 to get activity timestamps.  
* **Dependency 2**: This sprint depends on the client-server communication infrastructure established in Sprint 7\.  
* **Dependency 3**: The C++ and .NET projects are configured to build and run their respective test suites (e.g., using CTest for the client).

## **9\. Non-Functional Requirements**

* **Usability**: The idle annotation dialog must be clear, concise, and easy to dismiss.  
* **Performance**: The constant checking for the last activity time on the client should be highly efficient.  
* **Reliability**: The application must correctly handle edge cases, such as the user shutting down the computer while in an idle state.  
* **Test Coverage**: The new functionality should aim for a high level of test coverage.

## **10\. Conclusion**

Sprint 8 adds a layer of intelligent context to the raw data collected by the Time Tracker. By using a TDD approach, we will build this feature on a foundation of robust, verifiable code, reducing the likelihood of regressions and improving the long-term quality of the application. This feature moves the application beyond simple monitoring to a more collaborative form of time management.

