# Time Tracker Application: Product Requirements Document (PRD)

Version: 1.9

Date: June 14, 2025

Author: Gemini

Status: Draft

### 1. Introduction & Vision

This document outlines the product requirements for a new time-tracking
application. The vision is to create a robust, efficient, and
user-friendly tool for monitoring employee productivity, particularly in
a remote or multi-user environment. The application will accurately
track user activity, provide insightful analytics, and seamlessly
integrate with a central server for data storage and analysis.

The chosen technology stack of C++/Qt6 for the client, .NET Web API for
the server, and Angular for the web frontend will enable a
high-performance desktop application with a powerful, scalable backend
and a modern, responsive web dashboard.

### 2. Problem Statement

Organizations, especially those with remote employees or those utilizing
shared server environments (like Windows Server), lack an effective and
unobtrusive way to monitor employee productivity. Existing solutions can
be overly complex, resource-intensive, or lack the specific features
needed for multi-user session tracking. This project aims to solve this
by providing a lightweight, yet powerful, time-tracking tool that offers
detailed insights into user activity without disrupting workflows.

### 3. Target Audience

The primary users of this application are:

- **System Administrators:** Who need to deploy, manage, and monitor the
  application across multiple users and machines.

- **Managers/Team Leads (Staff Admins):** Who require insights into team
  productivity, project progress, and resource allocation.

- **Employees/End-Users (Workers):** Who will use the application to
  track their work and, in some cases, manually categorize their time.

### 4. Goals & Objectives

- **Business Goal:** To improve team productivity and accountability
  through accurate time and activity tracking.

- **Product Goal:** To deliver a reliable, efficient, and user-friendly
  time-tracking application that meets the specific needs of our
  organization.

- **Technical Goal:** To build a scalable and maintainable application
  using C++/Qt6, .NET Web API, and Angular within a monorepo structure.

### 5. Features & Functionality

  ------------------------------------------------------------------------
  **Feature ID**    **Feature Name**   **Description**   **Priority**
  ----------------- ------------------ ----------------- -----------------
  F-01              **Automatic Time   The application   High
                    Tracking**         will              
                                       automatically     
                                       track time when   
                                       the user is       
                                       active on their   
                                       computer. It will 
                                       run in the        
                                       background with a 
                                       system tray icon. 

  F-02              **Screenshot       Periodically      High
                    Capture**          capture           
                                       screenshots of    
                                       the user\'s       
                                       screen at         
                                       configurable      
                                       intervals.        

  F-03              **Mouse & Keyboard Monitor mouse and High
                    Tracking**         keyboard activity 
                                       to determine user 
                                       activity levels.  

  F-04              **Application      Track the         High
                    Tracking**         applications that 
                                       the user is       
                                       actively using.   

  F-05              **User             The desktop       High
                    Authentication**   application will  
                                       require users to  
                                       log in with their 
                                       credentials. This 
                                       authenticates the 
                                       session for       
                                       secure data       
                                       transmission and  
                                       API access.       

  F-06              **System Tray      The application   High
                    Operation**        will run          
                                       primarily from    
                                       the system tray,  
                                       with options to   
                                       show/hide the     
                                       main window and   
                                       exit the          
                                       application.      

  F-07              **Data             Securely transmit High
                    Transmission**     all tracked data  
                                       (time,            
                                       screenshots,      
                                       activity) to the  
                                       .NET Web API      
                                       server from an    
                                       authenticated     
                                       session.          

  F-08              **Idle Time        The application   Medium
                    Detection**        will detect       
                                       periods of user   
                                       inactivity and    
                                       allow for manual  
                                       categorization of 
                                       that time.        
  ------------------------------------------------------------------------

### 6. Technical Stack & Repository Structure

- **Desktop Application (app/):** C++/Qt6

- **Backend (backend/):** .NET Web API, C#, PostgreSQL

- **Frontend (frontend/):** Angular 19, TypeScript

- **Repository:** Monorepo (/app, /backend, /frontend)

### 7. Phased Development Plan

The project will be developed in three phases, with each phase broken
down into epics and sprints.

#### **Phase 1: Core Functionality & Authentication (Estimated Duration: 8 Weeks)**

**Goal:** To build the foundational tracking application with a secure,
testable authentication system.

**Epics:**

1.  **Sprint 1: Foundational Window and Tray Setup**

    - **Tasks:**

      - Client (C++/Qt): Create a basic application window.

      - Client (C++/Qt): Implement a system tray icon for the
        application.

      - Client (C++/Qt): Ensure the application launches with the system
        tray icon visible.

    - **Acceptance Criteria:** The application successfully launches,
      displaying both a main window and a functional system tray icon.

2.  **Sprint 2: Implemented \"Minimize to Tray\" Functionality**

    - **Tasks:**

      - Client (C++/Qt): Implement logic to minimize the main
        application window to the system tray.

      - Client (C++/Qt): Add context menu options to the system tray
        icon (e.g., \"Show Window,\" \"Exit\").

      - Client (C++/Qt): Ensure clicking the system tray icon can
        restore the main window.

    - **Acceptance Criteria:** The application window can be minimized
      to the system tray, and users can restore or exit the application
      via the system tray icon\'s context menu.

3.  **Authentication (Sprints 3-4):**

    - **Sprint 3: Backend Authentication with Seeded User**

      - **Tasks:**

        - **Backend (.NET):** Extend the User model to include a
          securely hashed password. Create a database seeding script
          that automatically adds one or more test users (e.g.,
          test.worker@example.com) on application startup in a
          development environment.

        - **Backend (.NET):** Implement a public /login API endpoint
          that accepts credentials, validates them against the database,
          and returns a JSON Web Token (JWT). Secure all other API
          endpoints to require a valid JWT.

      - **Acceptance Criteria:** The backend can authenticate the
        pre-defined, seeded user(s). All data-related endpoints are
        protected and return a 401 Unauthorized error if a valid token
        is not provided.

    - **Sprint 4: Client-Side Login & Token Management**

      - **Tasks:**

        - **Client (C++/Qt):** Create a login QDialog that is presented
          on application startup.

        - **Client (C++/Qt):** Implement the logic to call the /login
          endpoint. Securely store the returned JWT in memory for the
          session.

        - **Client (C++/Qt):** Implement a network request handler or
          interceptor that automatically includes the JWT as an
          Authorization: Bearer \<token\> header in all subsequent API
          requests.

      - **Acceptance Criteria:** The user must log in with the seeded
        credentials to use the application. All subsequent communication
        from the client to the server is authenticated.

4.  **Core Tracking Implementation (Sprints 5-7):**

    - **Sprint 5: Background Input Activity Tracking:** Implement
      low-level keyboard/mouse hooks to generate and store an activity
      metric.

    - **Sprint 6: Screenshot Capture Functionality:** Use a timer to
      capture and save screenshots locally.

    - **Sprint 7: Active Application Tracking:** Use a timer to log the
      active application and window title.

5.  **Server Communication (Sprint 8):**

    - **Sprint 8: Authenticated Data Ingestion Pipeline**

      - **Tasks:** Define backend data models and secure API endpoints
        for activity, screenshots, and app usage. Implement the
        screenshot upload service (including S3 integration). Implement
        the client-side service to send all tracked data to the secure,
        authenticated endpoints.

      - **Acceptance Criteria:** The backend correctly receives,
        validates, and stores all data from an authenticated client.
        Client cleans up local files after successful transmission.

    <!-- -->

    - 

#### Phase 2: Business Logic & Administration (Estimated Duration: 8 Weeks)

#### 

#### Goal: To implement core business logic, including project management and user administration, and build out the reporting dashboard.

#### 

#### Epics:

1.  **Advanced Tracking Features:**

    - **Sprint 9: Idle Time Detection and Annotation**

      - **Tasks:** Implement robust idle time detection mechanisms
        (e.g., no mouse/keyboard input for X minutes). Develop a
        client-side QDialog to prompt the user to categorize idle time
        (e.g., \"Lunch Break,\" \"Meeting,\" \"Personal Time\").
        Implement API endpoints to receive and store idle time
        annotations.

      - **Acceptance Criteria:** The application accurately detects idle
        periods. Users can categorize idle time through a clear,
        non-intrusive UI. Idle time data is securely stored on the
        server.

2.  **Project & Task Management:**

    - **Sprint 10: Backend for Project/Task Management & Assignments**

      - **Tasks:** Define database schemas for Projects and Tasks,
        including relationships to users. Implement RESTful API
        endpoints for creating, reading, updating, and deleting projects
        and tasks. Implement logic for assigning users to
        projects/tasks.

      - **Acceptance Criteria:** The backend can manage projects and
        tasks, assign them to users, and ensure data integrity. All
        endpoints are secured and accessible only with proper
        authentication.

    - **Sprint 11: Frontend for Admin Project Management**

      - **Tasks:** Develop an Angular web interface for administrators
        to create, edit, and view projects and tasks. Implement user
        assignment functionality within the project management UI.

      - **Acceptance Criteria:** Administrators can effectively manage
        projects and tasks through the web interface. Project and task
        assignments are accurately reflected and can be modified.

    - **Sprint 12: Client-Side Active Task Selection**

      - **Tasks:** Implement a client-side UI (e.g., a dropdown or small
        window) allowing employees to select their currently active
        project/task. Ensure this selection is sent with all tracked
        activity data.

      - **Acceptance Criteria:** Employees can easily select their
        active task. All subsequent tracked data (time, screenshots,
        activity) is associated with the selected task.

    - **Sprint 13: Viewing & Logging Time on Archived Projects**

      - **Tasks:** Implement backend logic to mark projects as
        \"archived\" and prevent new time logging against them while
        still allowing historical data viewing. Develop frontend
        functionality to view historical time logs for archived
        projects.

      - **Acceptance Criteria:** Archived projects are clearly
        distinguished. Users can view past time logs for archived
        projects but cannot log new time against them.

3.  **Administration & User Management:**

    - **Sprint 14: User Management and Role-Based Access** (This is
      where the UI for managing users will be built, replacing the
      seeded data approach for production).

      - **Tasks:** Develop a robust backend for user creation,
        modification, and deletion. Implement role-based access control
        (RBAC) to differentiate between System Admins, Staff Admins, and
        Workers. Create an Angular web interface for System Admins to
        manage users, assign roles, and reset passwords.

      - **Acceptance Criteria:** System Admins can fully manage user
        accounts and roles through the web interface. RBAC correctly
        restricts access to features based on user roles. The seeded
        user approach is no longer required for system functionality in
        production.

4.  **Server-Side Reporting & Frontend Dashboard:**

    - **Sprint 15: Basic Reporting Dashboard**

      - **Tasks:** Develop backend API endpoints to aggregate and query
        time tracking data (by user, project, date range). Create an
        Angular web dashboard displaying key metrics (e.g., total active
        time, time per project, idle time) with basic filtering options.

      - **Acceptance Criteria:** The dashboard accurately displays
        aggregated time tracking data. Users (Managers/Staff Admins) can
        filter data by basic criteria and gain initial insights into
        productivity.

    - **Sprint 16: Data Export**

      - **Tasks:** Implement backend functionality to export aggregated
        time tracking data into common formats (e.g., CSV, Excel).
        Develop a frontend UI button/feature to trigger these exports.

      - **Acceptance Criteria:** Users can successfully export time
        tracking data in chosen formats. Exported data is comprehensive
        and correctly reflects the filtered view on the dashboard.

> **Phase 3: User Experience & Scalability (Estimated Duration: 4
> Weeks)**
>
> **Goal:** To refine the user experience, improve scalability, and add
> user-centric features.
>
> **Epics:**

1.  **User Experience Enhancements:**

    - **Sprint 17: Manual Time Entry & Privacy Toggle**

      - **Tasks:** Implement a client-side interface for users to
        manually add time entries for periods they were not actively
        tracked or to correct inaccuracies. Add a \"Privacy Mode\"
        toggle in the client application that temporarily suspends all
        tracking (screenshots, activity, keyboard/mouse input) and
        clearly indicates when active.

      - **Acceptance Criteria:** Users can successfully add, edit, and
        delete manual time entries that integrate seamlessly with
        existing tracked data. The privacy toggle functions correctly,
        pausing and resuming tracking, with clear visual feedback to the
        user. All data captured during privacy mode is appropriately
        discarded or not generated.

    - **Sprint 18: Client UI Refinement**

      - **Tasks:** Conduct user feedback sessions to identify pain
        points and areas for improvement in the client application\'s
        user interface. Implement UI/UX improvements based on feedback,
        focusing on ease of use, clarity, and visual appeal. This may
        include improved notification management, clearer status
        indicators, and streamlined workflows.

      - **Acceptance Criteria:** The client application\'s UI is
        intuitive and user-friendly, as evidenced by positive user
        feedback and reduced support queries related to UI issues. All
        core functionalities are easily accessible and clearly
        presented.

2.  **Scalability & Performance:**

    - **Sprint 19: Platform Optimization**

      - **Tasks:** Profile the client application and backend services
        to identify performance bottlenecks. Implement optimizations
        such as efficient data serialization/deserialization, optimized
        database queries, image compression for screenshots, and network
        payload reduction. Refine background processes to minimize CPU
        and memory footprint on client machines.

      - **Acceptance Criteria:** The client application\'s resource
        consumption (CPU, RAM) is consistently low, even under heavy
        usage. Backend API response times are consistently fast, and
        database query performance is optimal. Screenshot storage and
        retrieval are efficient, with optimized file sizes.

    - **Sprint 20: Final Testing & Deployment Prep**

      - **Tasks:** Conduct comprehensive end-to-end testing, including
        integration testing between all components, performance testing
        under load, and security penetration testing. Prepare final
        deployment scripts and documentation for all environments
        (development, staging, production). Establish monitoring and
        alerting systems for production environments.

      - **Acceptance Criteria:** The application is stable, secure, and
        performs reliably under expected load. All known bugs are
        resolved or documented. The deployment process is fully
        automated and documented, allowing for smooth and repeatable
        releases. Monitoring systems are in place and configured to
        alert on critical issues.

### 8. Success Metrics

- **Adoption Rate:** The number of active users of the application.

- **Data Accuracy:** The reliability and accuracy of the tracked data.

- **User Feedback:** Positive feedback from managers and end-users
  regarding the application\'s usability and effectiveness.

- **Performance:** Low resource usage on client machines and fast
  response times from the server.

### 9. Risks & Mitigation

  -----------------------------------------------------------------------
  **Risk**          **Likelihood**    **Impact**        **Mitigation
                                                        Strategy**
  ----------------- ----------------- ----------------- -----------------
  **Technical       Medium            High              Allocate
  Challenges with                                       additional time
  Windows API**                                         for research and
                                                        development;
                                                        consult with
                                                        experts on
                                                        Windows API
                                                        integration.

  **Privacy         High              High              Be transparent
  Concerns**                                            with users about
                                                        what is being
                                                        tracked;
                                                        implement a
                                                        \"private time\"
                                                        feature; ensure
                                                        all data is
                                                        stored securely.

  **Performance     Medium            Medium            Optimize tracking
  Issues on Client                                      algorithms;
  Machines**                                            conduct thorough
                                                        performance
                                                        testing on
                                                        various hardware
                                                        configurations.

  **Scope Creep**   High              Medium            Strictly adhere
                                                        to the phased
                                                        development plan;
                                                        any new feature
                                                        requests will be
                                                        evaluated for
                                                        future releases.
  -----------------------------------------------------------------------
