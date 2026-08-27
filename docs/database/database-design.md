# Database Design

## Project

AI-Powered Student Skill Exchange and Peer Learning Platform

## 1. Database Overview

The platform uses a relational database to store student information, skills, learning requests, learning sessions, and feedback.

The database is designed using entity relationships so that student profiles can be connected with their skills, peer-learning requests, sessions, and feedback.

The application uses Entity Framework Core for database access and object-relational mapping.

## 2. Main Entities

### ApplicationUser

Stores the basic information of registered students.

Main attributes:

- User ID
- Full Name
- Bio
- Email
- Phone Number
- Authentication information

An ApplicationUser can have multiple skills and can send or receive multiple learning requests.

### Skill

Stores the skills available on the platform.

Main attributes:

- Skill ID
- Skill Name
- Category

A skill can be associated with multiple students through the StudentSkill entity.

### StudentSkill

Associates students with their skills.

Main attributes:

- StudentSkill ID
- Student ID
- Skill ID
- Skill Type
- Proficiency Level

Skill Type identifies whether the student wants to teach or learn the skill.

Proficiency Level represents the student's skill level, such as Beginner, Intermediate, or Expert.

### LearningRequest

Stores requests between students for peer learning.

Main attributes:

- Request ID
- Sender ID
- Receiver ID
- Skill ID
- Status
- Created At

The request status can be Pending, Accepted, or Rejected.

### LearningSession

Stores scheduled peer-learning sessions created from accepted learning requests.

Main attributes:

- Session ID
- Request ID
- Scheduled Time
- Status
- Meeting Link

The session status can be Scheduled, Completed, or Canceled.

### Feedback

Stores feedback and ratings provided after learning sessions.

Main attributes:

- Feedback ID
- Session ID
- Reviewer ID
- Rating
- Comments
- Created At

The rating is stored on a scale of 1 to 5.

## 3. Entity Relationships

### ApplicationUser → StudentSkill

One student can have multiple skills.

Relationship:

```text
ApplicationUser 1 ──────── * StudentSkill
```
StudentSkill acts as the association between students and skills.

### Skill → StudentSkill

One skill can be associated with multiple students.

Relationship:

```text
Skill 1 ──────── * StudentSkill
```
StudentSkill acts as the association between students and skills.

ApplicationUser → LearningRequest

A student can send multiple learning requests and can receive multiple learning requests.

Relationship:

ApplicationUser 1 ──────── * LearningRequest
             Sender

ApplicationUser 1 ──────── * LearningRequest
             Receiver
Skill → LearningRequest

A learning request is associated with a particular skill.

Relationship:

Skill 1 ──────── * LearningRequest
LearningRequest → LearningSession

A learning request can lead to a learning session.

Relationship:

LearningRequest 1 ──────── 0..1 LearningSession
LearningSession → Feedback

A learning session can have feedback associated with it.

Relationship:

LearningSession 1 ──────── * Feedback
ApplicationUser → Feedback

A student can provide feedback for learning sessions.

Relationship:

ApplicationUser 1 ──────── * Feedback
4. Overall Database Relationship
                    ApplicationUser
                    /       |       \
                   /        |        \
                  v         v         v
          StudentSkill  LearningRequest  Feedback
              ^              |
              |              |
              |              v
            Skill       LearningSession
                              |
                              v
                           Feedback
5. Key Design Principles
Primary keys uniquely identify each entity.
Foreign keys maintain relationships between entities.
Required fields are used where necessary to maintain data integrity.
StudentSkill avoids directly storing multiple skills inside the user record.
LearningRequest connects students with specific learning skills.
LearningSession is associated with an accepted learning request.
Feedback is associated with completed learning activities.
Entity Framework Core manages object-relational mapping between the application and database.
6. Database Technology

The project uses a relational database with Entity Framework Core.

The database schema is represented through C# model classes and Entity Framework Core migrations.

Database changes can be managed through migrations as the project evolves.





