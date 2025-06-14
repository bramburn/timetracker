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

### 5. Features & Functionality (MVP)

The Minimum Viable Product (MVP) will focus on the core features
required for effective time tracking:

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

  F-05              **Data             Securely transmit High
                    Transmission**     all tracked data  
                                       (time,            
                                       screenshots,      
                                       activity) to the  
                                       .NET Web API      
                                       server.           

  F-06              **System Tray      The application   High
                    Operation**        will run          
                                       primarily from    
                                       the system tray,  
                                       with options to   
                                       show/hide the     
                                       main window and   
                                       exit the          
                                       application.      

  F-07              **Remote Desktop   The application   High
                    Session Support**  will correctly    
                                       identify and      
                                       track individual  
                                       user sessions in  
                                       a multi-user      
                                       Windows Server    
                                       environment.      

  F-08              **User             Users will be     Medium
                    Identification**   identified by     
                                       their email       
                                       address, with no  
                                       separate login    
                                       credentials       
                                       required for the  
                                       desktop client.   

  F-09              **Idle Time        The application   Medium
                    Detection**        will detect       
                                       periods of user   
                                       inactivity and    
                                       allow for manual  
                                       categorization of 
                                       that time.        
  ------------------------------------------------------------------------

### 6. Technical Stack & Repository Structure

- **Desktop Application (app/):**

  - **Language:** C++

  - **Framework:** Qt6

  - **Platform:** Windows

- **Backend (backend/):**

  - **Framework:** .NET Web API

  - **Language:** C#

  - **Database:** PostgreSQL

- **Frontend (frontend/):**

  - **Framework:** Angular 19

  - **Language:** TypeScript

- **Repository:** The project will be managed in a single monorepo with
  the following structure:

  - /app: Contains the C++/Qt6 desktop client code.

  - /backend: Contains the .NET Web API server code.

  - /frontend: Contains the Angular 19 web dashboard code.

### 7. Phased Development Plan

The project will be developed in three phases, with each phase broken
down into epics and sprints.

#### **Phase 1: Core Functionality & MVP (Estimated Duration: 7 Weeks)**

**Goal:** To build and deploy the Minimum Viable Product with all
essential tracking features.

**Epics:**

1.  **Client Application Setup:**

    - **Sprint 1: Foundational Window and Tray Setup**

      - **Tasks:** Initialize the C++/Qt6 project, create a basic
        QMainWindow, implement QSystemTrayIcon with a context menu
        (\"Show/Hide\", \"Quit\").

      - **Acceptance Criteria:** The application runs, showing a main
        window and a functional system tray icon.

    - **Sprint 2: Implemented \"Minimize to Tray\" Functionality**

      - **Tasks:** Subclass QMainWindow, override closeEvent to hide the
        window and ignore the event, preventing the app from closing.

      - **Acceptance Criteria:** Clicking the main window\'s close
        button hides the window. The application remains active.

2.  **Core Tracking Implementation:**

    - **Sprint 3: Proof-of-Concept: Activity Logging to File**

      - **Tasks:**

        - Implement basic low-level keyboard and mouse hooks using the
          Windows API (SetWindowsHookEx).

        - For every event detected (e.g., a key press or mouse click),
          write a simple, timestamped log entry to a local text file
          (e.g., activity_log.txt).

        - This implementation does not need to be threaded or optimized
          at this stage; its purpose is purely to validate the hook
          mechanism.

      - **Acceptance Criteria:** When the application is running, a
        local log file is created and populated with timestamped entries
        for each keyboard and mouse event, proving the core capture
        mechanism works.

    - **Sprint 4: Background Input Activity Tracking**

      - **Tasks:** Refactor the proof-of-concept from Sprint 3. Run the
        hook callbacks in a separate, non-GUI thread (QThread) to
        prevent freezing the main application. Aggregate input events
        over a defined time period (e.g., 1 minute) to create a simple
        \"activity level\" metric (e.g., number of clicks + keystrokes).

      - **Acceptance Criteria:** The application correctly captures
        keyboard and mouse activity system-wide, even when minimized.
        The activity is quantified into a periodic metric without
        impacting UI responsiveness.

    - **Sprint 5: Screenshot Capture Functionality**

      - **Tasks:** Use a QTimer to trigger screen captures via Windows
        GDI, compress to JPEG, and save locally.

      - **Acceptance Criteria:** The application automatically takes and
        saves screenshots at a configured frequency.

    - **Sprint 6: Active Application Tracking**

      - **Tasks:** Use a QTimer and Windows API (GetForegroundWindow,
        etc.) to identify and log the active application name and window
        title.

      - **Acceptance Criteria:** The application accurately logs the
        active application at regular intervals.

3.  **Server Communication:**

    - **Sprint 7: Backend API and Data Ingestion Pipeline**

      - **Tasks:** Define backend data models and API endpoints for
        activity/screenshots. Implement screenshot upload service to
        process images (create thumbnail) and upload both versions to
        AWS S3, storing URLs in PostgreSQL. Implement client-side HTTP
        service (QNetworkAccessManager) to send batched data and upload
        screenshots.

      - **Acceptance Criteria:** The backend API correctly ingests and
        stores all data. The client successfully transmits data and
        cleans up local files upon confirmation.

#### **Phase 2: Business Logic & Administration (Estimated Duration: 8 Weeks)**

**Goal:** To implement core business logic, including project management
and user administration, and build out the reporting dashboard.

**Epics:**

1.  **Advanced Tracking Features:**

    - **Sprint 8: Idle Time Detection and Annotation**

2.  **Project & Task Management:**

    - **Sprint 9: Backend for Project/Task Management & Assignments**

    - **Sprint 10: Frontend for Admin Project Management**

    - **Sprint 11: Client-Side Active Task Selection**

    - **Sprint 12: Viewing & Logging Time on Archived Projects**

3.  **Administration & User Management:**

    - **Sprint 13: User Management and Role-Based Access**

4.  **Server-Side Reporting & Frontend Dashboard:**

    - **Sprint 14: Basic Reporting Dashboard**

    - **Sprint 15: Data Export**

#### **Phase 3: User Experience & Scalability (Estimated Duration: 4 Weeks)**

**Goal:** To refine the user experience, improve scalability, and add
user-centric features.

**Epics:**

1.  **User Experience Enhancements:**

    - **Sprint 16:** Implement manual time entry and a \"private time\"
      toggle to pause tracking.

    - **Sprint 17:** Refine the client UI for a more intuitive and
      non-intrusive experience.

2.  **Scalability & Performance:**

    - **Sprint 18:** Optimize client resource usage and server
      performance for a large number of users.

    - **Sprint 19:** Conduct thorough testing and bug fixing before a
      wider rollout.

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
