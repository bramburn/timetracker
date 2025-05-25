Employee Activity Monitor - Phase 1, Sprint 2 Backlog
Introduction

This document provides a detailed backlog for Phase 1, Sprint 2 of the Employee Activity Monitor project. Building upon the core data capture and initial single-record submission established in Sprint 1, this sprint focuses on implementing batch data submission to the configurable Pipedream endpoint at a defined interval (1 minute) and ensuring that successfully uploaded data is subsequently deleted from the local SQLite database to manage storage space. This aligns with the requirement to gather smaller amounts of data before sending it in patches.

The backlog is structured to provide clear user stories, actionable tasks, file relationships, a list of files to be created, acceptance criteria, a testing plan, identified assumptions and dependencies, and relevant non-functional requirements.
User Stories

    User Story 13: Timed Batch Data Upload to Pipedream

        Description: As the monitoring system, I want to collect activity records locally for a defined interval (e.g., 1 minute) and then transmit these collected records as a single batch (JSON array) to the configurable Pipedream endpoint, so that data is sent efficiently in patches rather than individually.

        Actions to Undertake:

            Configuration for Batch Interval: Add a new setting in appsettings.json for PipedreamBatchIntervalMinutes (default: 1 minute).

            Data Buffering/Queueing: Implement a mechanism (e.g., a ConcurrentQueue<ActivityDataModel> or a List<ActivityDataModel>) within ActivityLogger or a new BatchProcessor class to temporarily store activity records as they are captured.

            Timed Trigger: Introduce a Timer or a BackgroundService within the Windows Service that triggers every PipedreamBatchIntervalMinutes.

            Batch Creation: When the timer triggers, retrieve all buffered records, clear the buffer, and create a batch (e.g., List<ActivityDataModel>).

            Batch Serialization: Serialize the list of ActivityDataModel objects into a single JSON array.

            Batch Transmission: Modify PipedreamClient.cs to accept and send a batch (list) of records as a JSON array to the Pipedream endpoint.

        References between Files:

            appsettings.json: New PipedreamBatchIntervalMinutes setting.

            Program.cs: Initializes the new batch processing component.

            ActivityLogger.cs: Modifies LogActivity method to add records to a buffer instead of immediately sending to PipedreamClient.

            BatchProcessor.cs (New File): Contains the timer, buffering logic, and triggers PipedreamClient for batch upload.

            PipedreamClient.cs: Modified UploadDataAsync to accept IEnumerable<ActivityDataModel> and serialize as a JSON array.

            ActivityDataModel.cs: The structure of individual records within the batch.

        Acceptance Criteria:

            The application collects activity data for the configured PipedreamBatchIntervalMinutes.

            At the end of each interval, all collected records are sent as a single JSON array to Pipedream.

            The Pipedream endpoint receives a JSON array containing multiple activity records.

            No individual records are sent outside of the batching mechanism.

        Testing Plan:

            Test Case 1: Verify Batching Interval and Content

                Test Data: Set PipedreamBatchIntervalMinutes to 1. Generate continuous activity for 2-3 minutes.

                Expected Result: Pipedream receives JSON payloads containing multiple records at approximately 1-minute intervals. The number of records in each batch corresponds to the activity generated within that minute.

                Testing Tool: Pipedream inspection, application logs.

            Test Case 2: Verify Empty Batch Handling

                Test Data: Set PipedreamBatchIntervalMinutes to 1. Leave the system idle for 2-3 minutes.

                Expected Result: Pipedream receives empty arrays or no requests during idle periods (depending on implementation decision: send empty array or skip if no data).

                Testing Tool: Pipedream inspection, application logs.

    User Story 14: Local Data Deletion After Successful Batch Upload

        Description: As the monitoring system, I want records from the local SQLite database that have been successfully included in a batch upload to the Pipedream endpoint to be deleted from the local database, so that local storage space is managed and only unsynced data remains.

        Actions to Undertake:

            Mark Records for Sync: Add an IsSynced boolean flag (default false) and BatchId (GUID) to the ActivityLogs table in TimeTracker.db.

            Update Records on Batch Creation: When a batch is prepared for upload, update the IsSynced flag to true and assign a BatchId to these records in the local database.

            Transactional Deletion: After a successful HTTP 2xx response from Pipedream for a batch, implement a transactional deletion operation in SQLiteDataAccess.cs to remove all records associated with that BatchId where IsSynced is true.

            Error Handling for Deletion: If the deletion fails, log the error and ensure the IsSynced flag remains true for those records, preventing re-upload but allowing manual intervention if needed.

        References between Files:

            TimeTracker.db: ActivityLogs table schema updated with IsSynced (BIT) and BatchId (TEXT/GUID).

            ActivityDataModel.cs: Add IsSynced (bool) and BatchId (Guid) properties.

            BatchProcessor.cs: Assigns BatchId to records and updates IsSynced flag before sending to PipedreamClient. Calls SQLiteDataAccess for deletion after successful upload.

            SQLiteDataAccess.cs: New methods for bulk updating IsSynced and BatchId, and for transactional bulk deletion based on BatchId.

            PipedreamClient.cs: Returns success/failure status of the batch upload.

        Acceptance Criteria:

            Records that are part of a successfully uploaded batch are deleted from the local SQLite database.

            Records are not deleted if the Pipedream upload fails.

            The deletion process is transactional, ensuring data consistency.

            Local database size reflects the deletion of synced records over time.

        Testing Plan:

            Test Case 1: Successful Upload and Deletion

                Test Data: Generate activity, allow a batch to be sent successfully to Pipedream.

                Expected Result: Records in the local database corresponding to the sent batch are deleted.

                Testing Tool: Pipedream inspection (to confirm receipt), local database inspection (to confirm deletion).

            Test Case 2: Failed Upload and No Deletion

                Test Data: Generate activity, then configure Pipedream URL to be invalid or block network access to simulate failure.

                Expected Result: Records remain in the local database (with IsSynced=true and BatchId assigned), and errors are logged.

                Testing Tool: Pipedream inspection (to confirm no receipt), local database inspection (to confirm records still present), application logs.

            Test Case 3: Database Consistency on Deletion Failure

                Test Data: Simulate a database error during the deletion phase after a successful Pipedream upload.

                Expected Result: The deletion transaction rolls back, records remain in the database (with IsSynced=true), and an error is logged.

                Testing Tool: Introduce controlled database error, local database inspection, application logs.

Actions to Undertake (Consolidated)

    Configure Batch Interval: Add PipedreamBatchIntervalMinutes to appsettings.json.

    Update ActivityDataModel: Add IsSynced (bool) and BatchId (Guid) properties.

    Update TimeTracker.db Schema: Add IsSynced and BatchId columns to ActivityLogs table.

    Implement Data Buffering: Modify ActivityLogger to store records in a temporary buffer.

    Create BatchProcessor.cs:

        Implement a Timer or BackgroundService to trigger batch processing at the defined interval.

        Logic to pull records from the buffer, assign BatchId, and update IsSynced in the local DB.

        Call PipedreamClient to send the batch.

        Handle PipedreamClient response to trigger local deletion.

    Modify PipedreamClient.cs:

        Update UploadDataAsync to accept IEnumerable<ActivityDataModel>.

        Implement JSON serialization of the list into a JSON array.

    Update SQLiteDataAccess.cs:

        Add methods for bulk updating IsSynced and BatchId flags.

        Add methods for transactional bulk deletion of records based on BatchId.

    Error Handling: Ensure robust error handling for network failures (Pipedream) and database operations (deletion).

    Refinement & Testing: Thoroughly test all new features (unit, integration, system tests) to ensure correct batching, timing, and transactional deletion.

References between Files

    timetracker-monorepo/ (Root):

        timetracker.sln: Links desktop-app/TimeTracker.DesktopApp/TimeTracker.DesktopApp.csproj.

    desktop-app/TimeTracker.DesktopApp/:

        TimeTracker.DesktopApp.csproj: Updated to reflect new files and dependencies.

        Program.cs: Initializes BatchProcessor.

        ActivityDataModel.cs: Updated with IsSynced and BatchId.

        ActivityLogger.cs: Modified to buffer records.

        BatchProcessor.cs (New File): Orchestrates batching, calls ActivityLogger (to get buffered data), PipedreamClient, and SQLiteDataAccess.

        PipedreamClient.cs: UploadDataAsync updated for batch payload.

        SQLiteDataAccess.cs: New methods for bulk update and bulk delete.

        appsettings.json: New PipedreamBatchIntervalMinutes setting.

        TimeTracker.db: ActivityLogs table schema updated.

    External Dependencies:

        Windows OS (for Windows Service).

        .NET 8 SDK.

        Pipedream HTTP endpoint.

List of Files being Created

    File 23: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/BatchProcessor.cs

        Purpose: Manages the timed collection and batching of activity data for Pipedream upload, and triggers local data deletion.

        Contents: Contains the timer, a queue/list for buffering ActivityDataModel objects, logic to form batches, assign BatchId, update IsSynced flag, call PipedreamClient for upload, and call SQLiteDataAccess for deletion.

        Relationships: Reads appsettings.json, interacts with ActivityLogger (to retrieve buffered data), PipedreamClient, and SQLiteDataAccess.

    Updated Files (from Phase 1):

        TimeTracker.DesktopApp.csproj: Updated to include BatchProcessor.cs.

        Program.cs: Modified to instantiate and start BatchProcessor.

        ActivityDataModel.cs: Added IsSynced (bool) and BatchId (Guid) properties.

        ActivityLogger.cs: Modified logging method to buffer records instead of immediate Pipedream calls.

        PipedreamClient.cs: UploadDataAsync method signature and implementation changed to handle IEnumerable<ActivityDataModel>.

        SQLiteDataAccess.cs: Added new methods for bulk updating IsSynced/BatchId and transactional bulk deletion.

        appsettings.json: Added PipedreamBatchIntervalMinutes setting.

        TimeTracker.db: ActivityLogs table schema altered to include IsSynced and BatchId columns.

Acceptance Criteria (Consolidated)

    Batch Upload:

        Activity data is buffered locally for the configured interval (default 1 minute).

        At the end of each interval, all buffered records are compiled into a batch.

        The batch is serialized as a JSON array and sent as a single HTTP POST request to the Pipedream endpoint.

        Pipedream successfully receives and processes the JSON array containing multiple records.

    Local Data Deletion:

        Upon successful (HTTP 2xx) receipt of a batch by Pipedream, all records corresponding to that batch are atomically deleted from the local ActivityLogs table in TimeTracker.db.

        If the Pipedream upload fails, the records are not deleted from the local database, and errors are logged.

        The deletion process is transactional, ensuring data consistency even if an error occurs during deletion.

        The local database size is actively managed by deleting synced records.

Testing Plan

The testing plan for Phase 1, Sprint 2 will focus heavily on the new batching and deletion logic.

    Unit Testing:

        Scope: BatchProcessor.cs (mocking ActivityLogger, PipedreamClient, SQLiteDataAccess interactions), PipedreamClient.cs (verifying JSON array serialization), SQLiteDataAccess.cs (testing bulk update and transactional bulk delete methods).

        Tools: NUnit/xUnit, Moq.

        Test Case 1: BatchProcessor - Batch Formation and Trigger

            Test Data: Simulate ActivityLogger buffering records, trigger BatchProcessor timer.

            Expected Result: BatchProcessor correctly forms a batch and calls PipedreamClient with the batched data.

            Testing Tool: NUnit.

        Test Case 2: SQLiteDataAccess - Bulk Update IsSynced/BatchId

            Test Data: List of ActivityDataModel objects with assigned BatchId.

            Expected Result: Corresponding records in a mock/in-memory SQLite DB are updated with IsSynced=true and the correct BatchId.

            Testing Tool: NUnit.

        Test Case 3: SQLiteDataAccess - Transactional Bulk Delete

            Test Data: BatchId and a mock/in-memory SQLite DB with records marked IsSynced=true for that BatchId.

            Expected Result: Records are deleted. Test rollback scenario by simulating an error during deletion.

            Testing Tool: NUnit.

    Integration Testing:

        Scope: End-to-end flow from data capture, buffering, batching, Pipedream upload, and subsequent local deletion.

        Tools: NUnit/xUnit, actual SQLite database, real Pipedream endpoint (for success cases), local web server (for controlled failure simulation).

        Test Case 4: Full Batch Upload and Deletion Cycle (Success)

            Test Data: Generate continuous activity for several minutes, allow service to run normally.

            Expected Result: Pipedream receives multiple batches. For each successfully sent batch, the corresponding records are deleted from the local DB.

            Testing Tool: Pipedream inspection, local database inspection, application logs.

        Test Case 5: Batch Upload Failure and No Deletion

            Test Data: Generate activity, then make Pipedream endpoint unreachable/return error.

            Expected Result: Batches are attempted, errors are logged, but records are not deleted from the local DB. Records should retain IsSynced=true and BatchId to indicate they were attempted to be synced.

            Testing Tool: Network blocking/Pipedream misconfiguration, local database inspection, application logs.

    System Testing (Manual & Automated):

        Scope: Validate the entire Windows Service with new batching and deletion features on target Windows environments.

        Tools: Windows Services Manager, Task Manager, Performance Monitor, SQLite Browser/CLI, Pipedream logs, network monitoring tools.

        Test Case 6: Long-Running Batching and Deletion

            Test Data: Run the service for an extended period (e.g., 24 hours) with continuous user activity.

            Expected Result: Local database size remains stable or grows slowly, indicating successful deletion of synced data. Pipedream shows consistent batch receipts.

            Testing Tool: Long-term monitoring of disk space, Pipedream logs, application logs.

        Test Case 7: Network Fluctuation Resilience

            Test Data: Simulate intermittent network connectivity issues (e.g., enable/disable Wi-Fi, block/unblock Pipedream URL).

            Expected Result: The service handles network fluctuations gracefully, retries uploads, and deletes data only upon successful delivery.

            Testing Tool: Manual network manipulation, application logs, local database inspection.

Assumptions and Dependencies

    Assumptions:

        The Pipedream endpoint is capable of receiving and processing JSON arrays as batch payloads.

        The PipedreamBatchIntervalMinutes (1 minute) is an acceptable frequency for both data freshness and network overhead.

        The volume of data collected within a 1-minute interval will not exceed reasonable batch size limits for HTTP requests.

        The Windows Service has necessary permissions to perform database operations and network requests.

    Dependencies:

        All dependencies from Phase 1, Sprint 1.

        Microsoft.Extensions.Hosting.WindowsServices (for service hosting).

        System.Threading.Channels or System.Collections.Concurrent (for buffering/queuing).

Non-Functional Requirements

    Performance:

        The batching and deletion process should not introduce significant spikes in CPU or memory usage.

        Network traffic should be optimized by sending data in batches rather than individual records.

        Local database operations (updates, deletions) should be efficient and not cause I/O bottlenecks.

    Reliability:

        Data integrity must be maintained: records are only deleted locally after confirmed successful upload to Pipedream.

        The batch processing mechanism should be resilient to temporary network or database issues, with appropriate retry logic.

    Storage Management:

        The local SQLite database size should be effectively managed by the deletion of synced records, preventing unbounded growth.

    Maintainability:

        The batching logic should be clearly separated into its own component (BatchProcessor.cs).

        Configuration for batching interval should be easily modifiable via appsettings.json.

Conclusion

Phase 1, Sprint 2 marks a significant step towards efficient data management and transmission for the Employee Activity Monitor. By implementing timed batch uploads and transactional local data deletion, the system will become more robust, resource-efficient, and capable of handling continuous data flow while managing local storage. This sprint's successful completion will solidify the MVP's data handling capabilities, preparing the groundwork for future phases.