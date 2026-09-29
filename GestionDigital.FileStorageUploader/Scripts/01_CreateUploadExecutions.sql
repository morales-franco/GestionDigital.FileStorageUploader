-- Apply to the FileStorage database before deploying the audited uploader.
IF OBJECT_ID(N'dbo.UploadExecutions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UploadExecutions
    (
        Id INT IDENTITY(1,1) NOT NULL ,
        StartedAt DATETIME NOT NULL,
        FinishedAt DATETIME NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_UploadExecutions_Status DEFAULT 'RUNNING',
        TargetStorageTypeId VARCHAR(50) NOT NULL,
        ExpectedFilesCount INT NOT NULL,
        UploadedFilesCount INT NOT NULL,
        ErrorMessage NVARCHAR(1000) NULL,
        CreatedDate DATETIME NOT NULL,
        CONSTRAINT PK_UploadExecutions PRIMARY KEY (Id),
        CONSTRAINT DF_UploadExecutions_CreatedDate DEFAULT GETDATE()
    );
    
    CREATE INDEX IX_UploadExecutions_StartedAt ON dbo.UploadExecutions (StartedAt);
END;
