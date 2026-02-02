```mermaid 

---
title: Class Diagram
---

classDiagram

    class Common {
        + T Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
    }

    class User {
        + string Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + string FirstName
        + string LastName
        + DateOnly Birthday
        + string Description
        + json<TagsEnum> ValueTags
        + string Email
        + string HashedPassword
    }

    class Questionnaire {
        + int Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + string UserId
        + json Quiz
    }

    class QuestionPicture {
        + int Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + int QuestionnaireId
        + string QPictureBase64
    }

    class ProfilePicture {
        + int Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + string UserId
        + int<PictureTypeEnum> PictureType
        + string PPictureBase64
    }

    class RefreshToken {
        + string Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + string UserId
        + DateTime ExpiryDate
        + bool IsRevoked
    }

    class UserRole {
        + int Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + string UserId
        + int<RoleEnum> Role
    }

    class QuestionScore {
        + int Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + string PlayerId
        + string QuizOwnerId
        + int MatchProcent
        + bool IsUserVisible
    }

    class Participant {
        + int Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + string UserId
        + string ChatRoomId
    }

    class ChatRoom {
        + string Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + string Name
    }

    class ChatMessage {
        + int Id
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + string ChatRoomId
        + string SenderId
        + string MessageContent
    }

Common --|> User
Common --|> Questionnaire
Common --|> QuestionPicture
Common --|> ProfilePicture
Common --|> RefreshToken
Common --|> UserRole
Common --|> QuestionScore
Common --|> Participant
Common --|> ChatRoom
Common --|> ChatMessage

Participant --> User
Participant --> ChatRoom
ChatMessage --> ChatRoom
ChatMessage --> User

UserRole --> User
QuestionPicture --> Questionnaire
QuestionPicture --> User
Questionnaire --> User
ProfilePicture --> User
RefreshToken --> User
QuestionScore --> User
