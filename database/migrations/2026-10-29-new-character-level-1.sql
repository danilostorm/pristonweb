-- StormPT - nivel inicial dos NOVOS personagens = 1
-- Banco: StartUserDB | Tabela: dbo.Level | Campo: StartLevel
-- Fundamentacao: OnSever.cpp StartUser_PresentItem_Init() carrega dbo.Level
-- e envia nStartUserLevel no OPCODE_FAILRECORDDATA ao criar personagem novo.
-- IMPORTANTE: Fazer BACKUP DATABASE antes, executar no SQL Express da VM Games,
-- e reiniciar Server33.exe somente quando nao houver jogadores conectados.
-- Nao altera personagens existentes nem seus saves.
USE [StartUserDB];
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF (SELECT COUNT_BIG(*) FROM dbo.[Level] WITH (UPDLOCK, HOLDLOCK)) <> 1
BEGIN
    ROLLBACK TRANSACTION;
    THROW 51000, 'Esperada exatamente 1 linha em StartUserDB.dbo.Level', 1;
END;
DECLARE @Current int = (SELECT TOP(1) StartLevel FROM dbo.[Level] WITH (UPDLOCK, HOLDLOCK));
IF @Current = 100
BEGIN
    UPDATE dbo.[Level] SET StartLevel=1 WHERE StartLevel=100;
    IF @@ROWCOUNT <> 1
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51001, 'Atualizacao do nivel inicial nao afetou exatamente uma linha', 1;
    END;
END
ELSE IF @Current <> 1
BEGIN
    ROLLBACK TRANSACTION;
    THROW 51002, 'Nivel inicial inesperado: inspecione antes de alterar', 1;
END;
COMMIT TRANSACTION;
SELECT StartLevel FROM dbo.[Level];
