USE [master];
GO

RESTORE DATABASE AdventureWorksLT2025
FROM DISK = '/var/opt/mssql/backup/AdventureWorksLT2025.bak'
WITH
    MOVE N'AdventureWorksLT2022_Data' TO '/var/opt/mssql/data/AdventureWorksLT2025.mdf',
    MOVE N'AdventureWorksLT2022_Log' TO '/var/opt/mssql/log/AdventureWorksLT2025.ldf',
    REPLACE;