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
        + int PlayId
        + DateTime CreatedAt
        + DateTime UpdatedAt
        + string UserId
        + string QuizOwnerId
        + int MatchProcent
        + bool IsUserVisible
    }

Common --|> User
Common --|> Questionnaire
Common --|> QuestionPicture
Common --|> ProfilePicture
Common --|> RefreshToken
Common --|> UserRole
Common --|> QuestionScore

UserRole --> User
Questionnaire --> User
QuestionPicture --> User
ProfilePicture --> User
RefreshToken --> User
QuestionScore --> User

QuestionPicture --> Questionnaire