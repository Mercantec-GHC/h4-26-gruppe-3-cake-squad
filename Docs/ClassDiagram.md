```mermaid 

---
title: Class Diagram
---

classDiagram

    class User {
        + string Id
        + string FirstName
        + string LastName
        + string Birthday
        + string Description
        + string Email
        + string PasswordHash
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

Questionnaire --> User
QuestionPicture --> User
ProfilePicture --> User

QuestionPicture --> Questionnaire