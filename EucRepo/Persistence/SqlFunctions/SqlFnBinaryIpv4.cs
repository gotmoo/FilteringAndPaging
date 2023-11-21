namespace EucRepo.Persistence.SqlFunctions;

public class SqlFnBinaryIpv4 : SqlProgrammability
{
    public SqlFnBinaryIpv4()
    {
        Name = "fnBinaryIpv4";
        ProgramType = SqlProgramTypes.Function;
        Versions = new List<SqlVersion>
        {
            new()
            {
                Version = 1,
                Content = @"
-- =============================================
-- Author:		Johan Greefkes
-- Create date: 2022-08-15
-- Description:	Translate an IPv4 address into binary representation
-- =============================================
CREATE OR ALTER FUNCTION [dbo].[fnBinaryIPv4](@ip AS VARCHAR(15)) RETURNS BINARY(4)
AS
BEGIN
    DECLARE @bin AS BINARY(4)

    SELECT @bin = CAST( CAST( PARSENAME( @ip, 4 ) AS INTEGER) AS BINARY(1))
            + CAST( CAST( PARSENAME( @ip, 3 ) AS INTEGER) AS BINARY(1))
            + CAST( CAST( PARSENAME( @ip, 2 ) AS INTEGER) AS BINARY(1))
            + CAST( CAST( PARSENAME( @ip, 1 ) AS INTEGER) AS BINARY(1))
    RETURN @bin
END
"
            }
        };
    }
}