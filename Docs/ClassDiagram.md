```mermaid 

---
title: Class Diagram
---

classDiagram

    class User {
        + string Id
        + string FirstName
        + string LastName
        + DateOnly Birthday
        + string Description
        + string Email
        + string HashedPassword
        + DateOnly RegistrationDate
        + json<TagsEnum> ValueTags
    }

    class Questionnaire {
        + int Id
        + string UserId
        + json Questions
    }

    class QuestionPicture {
        + int Id
        + int QuestionnaireId
        + string QPictureBase64
    }

    class ProfilePicture {
        + int Id
        + string UserId
        + int<PictureTypeEnum> PictureType
        + string PPictureBase64
    }

    class RefreshToken {
        + string Id
        + string UserId
        + DateTime ExpiryDate
        + bool IsRevoked
    }

Questionnaire --> User
QuestionPicture --> User
ProfilePicture --> User
RefreshToken --> User

QuestionPicture --> Questionnaire