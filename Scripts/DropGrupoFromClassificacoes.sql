IF COL_LENGTH('dbo.FutPlay_Classificacoes', 'Grupo') IS NOT NULL
BEGIN
    ALTER TABLE dbo.FutPlay_Classificacoes DROP COLUMN Grupo;
END
ELSE IF COL_LENGTH('FutPlay_Classificacoes', 'Grupo') IS NOT NULL
BEGIN
    ALTER TABLE FutPlay_Classificacoes DROP COLUMN Grupo;
END;
