CREATE DATABASE ApplicationDB;
GO

CREATE TABLE [dbo].[SubRedits]
(
  [Id] INT NOT NULL PRIMARY KEY,
  [DisplayName] varchar(100),
  [Reference] varchar(100),
  [Description] varchar(255)
)
GO
