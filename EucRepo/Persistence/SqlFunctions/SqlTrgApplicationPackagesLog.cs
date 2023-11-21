namespace EucRepo.Persistence.SqlFunctions;

public class SqlTrgApplicationPackagesLog:SqlProgrammability
{
    public SqlTrgApplicationPackagesLog()
    {
        Name = "trg_ApplicationPackages_Log";
        ProgramType = SqlProgramTypes.Trigger;
        Versions = new List<SqlVersion>
        {
            new()
            {
                Version = 1,
                Content = """
						-- =============================================
						-- Author:		Johan Greefkes
						-- Create date: 2023-07-26
						-- Description:	Custom logging trigger that also updates packaging time stamps.
						--              Special consideration is made to not touch logged time stamps when package is put on hold or cancelled.
						--              Package availability is also set based on the selected status.
						--				Both the source table as well as the logged entry are updated on an "update" action
						-- =============================================
						CREATE OR ALTER   TRIGGER [dbo].[trg_ApplicationPackages_Log]
						   ON  [dbo].[ApplicationPackages]
						   AFTER DELETE, INSERT, UPDATE
						AS 
						BEGIN
							SET NOCOUNT ON;
							DECLARE @TimeStamp as DateTime = GETUTCDATE()
						DECLARE @SpecialBypass as bit = 0
						-- insert case
						IF EXISTS (SELECT * FROM inserted) AND NOT EXISTS (SELECT * FROM deleted)
						BEGIN	INSERT INTO LogApplicationPackages ( [AdGroup], [ApplicationId], [BuatAvailable], [CancelDate], [ClarificationDate], [DcPair], [DeploymentGroup], [HasDependencies], [HasUserConfigSettings], [HoldDate], [Id], [IsAvailable], [Modified], [ModifiedBy], [OnHoldBy], [PackageUseId], [PackagingCompleted], [PackagingStarted], [PackagingSubmitted], [ProductionAvailable], [StatusId], [Type], [UatAvailable], [UatSignedOff], [Variant], [XenAppName], [LogAction], [LogTime]) select  [AdGroup], [ApplicationId], [BuatAvailable], [CancelDate], [ClarificationDate], [DcPair], [DeploymentGroup], [HasDependencies], [HasUserConfigSettings], [HoldDate], [Id], [IsAvailable], [Modified], [ModifiedBy], [OnHoldBy], [PackageUseId], [PackagingCompleted], [PackagingStarted], [PackagingSubmitted], [ProductionAvailable], [StatusId], [Type], [UatAvailable], [UatSignedOff], [Variant], [XenAppName],'Insert', @TimeStamp FROM inserted END

						-- update case
						IF EXISTS (select * FROM inserted) AND  EXISTS (SELECT * FROM deleted)
						BEGIN  
							--Update source table with status time stamps
							UPDATE ap 
							SET 
							@SpecialBypass = case when aps.IsOnHold = 1 OR aps.IsClarificationRequested = 1 OR aps.IsCancelled = 1 THEN 1 ELSE 0 END
							, PackagingSubmitted = dbo.fnReturnStatusDate(ap.PackagingSubmitted, aps.IsPackagingRequested, @SpecialBypass, @TimeStamp) 
							, PackagingStarted = dbo.fnReturnStatusDate(ap.PackagingStarted, aps.IsPackagingStarted, @SpecialBypass, @TimeStamp) 
							, PackagingCompleted = dbo.fnReturnStatusDate(ap.PackagingCompleted, aps.IsPackagingCompleted, @SpecialBypass, @TimeStamp) 
							, UatAvailable = dbo.fnReturnStatusDate(ap.UatAvailable, aps.IsUatAvailable, @SpecialBypass, @TimeStamp) 
							, UatSignedOff = dbo.fnReturnStatusDate(ap.UatSignedOff, aps.IsUatSignedOff, @SpecialBypass, @TimeStamp) 
							, BuatAvailable = dbo.fnReturnStatusDate(ap.BuatAvailable, aps.IsBuatAvailable, @SpecialBypass, @TimeStamp)
							, ProductionAvailable = dbo.fnReturnStatusDate(ap.ProductionAvailable, aps.IsProductionAvailable, @SpecialBypass, @TimeStamp)
							, HoldDate = dbo.fnReturnStatusDate(ap.HoldDate, aps.IsOnHold, 0, @TimeStamp)
							, OnHoldBy = CASE WHEN aps.IsOnHold = 1 AND ap.OnHoldBy IS NULL THEN ap.ModifiedBy ELSE NULL END
							, ClarificationDate = dbo.fnReturnStatusDate(ap.ClarificationDate, aps.IsClarificationRequested, 0, @TimeStamp)
							, CancelDate = dbo.fnReturnStatusDate(ap.CancelDate, aps.IsCancelled, 0, @TimeStamp)
							, IsAvailable = aps.IsProductionAvailable
							FROM  ApplicationPackages ap
							JOIN ApplicationPackageStatus aps ON ap.StatusId = aps.id
							WHERE ap.id IN (SELECT Id FROM inserted)

							--Insert record into logging table and apply the same status time stamps
							INSERT INTO LogApplicationPackages ( [AdGroup], [ApplicationId], [BuatAvailable], [CancelDate], [ClarificationDate], [DcPair], [DeploymentGroup], [HasDependencies], [HasUserConfigSettings], [HoldDate], [Id], [IsAvailable], [Modified], [ModifiedBy], [OnHoldBy], [PackageUseId], [PackagingCompleted], [PackagingStarted], [PackagingSubmitted], [ProductionAvailable], [StatusId], [Type], [UatAvailable], [UatSignedOff], [Variant], [XenAppName], [LogAction], [LogTime]) 
							SELECT  [AdGroup], [ApplicationId]
							, dbo.fnReturnStatusDate(ap.[BuatAvailable], aps.IsBuatAvailable, CASE WHEN aps.IsOnHold = 1 OR aps.IsClarificationRequested = 1 OR aps.IsCancelled = 1 THEN 1 ELSE 0 END, @TimeStamp) 
							, dbo.fnReturnStatusDate(ap.[CancelDate], aps.IsCancelled, 0, @TimeStamp) 
							, dbo.fnReturnStatusDate(ap.[ClarificationDate], aps.IsClarificationRequested, 0, @TimeStamp) 
							, [DcPair]
							, [DeploymentGroup]
							, [HasDependencies]
							, [HasUserConfigSettings]
							, dbo.fnReturnStatusDate(ap.[HoldDate], aps.IsOnHold, 0, @TimeStamp) 
							, ap.[Id]
							, aps.IsProductionAvailable
							, [Modified]
							, [ModifiedBy]
							, CASE WHEN aps.IsOnHold = 1 AND ap.OnHoldBy IS NULL THEN ap.ModifiedBy ELSE NULL END 
							, [PackageUseId]
							, dbo.fnReturnStatusDate(ap.[PackagingCompleted], aps.IsPackagingCompleted, CASE WHEN aps.IsOnHold = 1 OR aps.IsClarificationRequested = 1 OR aps.IsCancelled = 1 THEN 1 ELSE 0 END, @TimeStamp) 
							, dbo.fnReturnStatusDate(ap.[PackagingStarted], aps.IsPackagingStarted, CASE WHEN aps.IsOnHold = 1 OR aps.IsClarificationRequested = 1 OR aps.IsCancelled = 1 THEN 1 ELSE 0 END, @TimeStamp) 
							, dbo.fnReturnStatusDate(ap.[PackagingSubmitted], aps.IsPackagingRequested, CASE WHEN aps.IsOnHold = 1 OR aps.IsClarificationRequested = 1 OR aps.IsCancelled = 1 THEN 1 ELSE 0 END, @TimeStamp) 
							, dbo.fnReturnStatusDate(ap.[ProductionAvailable], aps.IsProductionAvailable, CASE WHEN aps.IsOnHold = 1 OR aps.IsClarificationRequested = 1 OR aps.IsCancelled = 1 THEN 1 ELSE 0 END, @TimeStamp) 
							, [StatusId]
							, [Type]
							, dbo.fnReturnStatusDate(ap.[UatAvailable], aps.IsUatAvailable, CASE WHEN aps.IsOnHold = 1 OR aps.IsClarificationRequested = 1 OR aps.IsCancelled = 1 THEN 1 ELSE 0 END, @TimeStamp) 
							, dbo.fnReturnStatusDate(ap.[UatSignedOff], aps.IsUatSignedOff, CASE WHEN aps.IsOnHold = 1 OR aps.IsClarificationRequested = 1 OR aps.IsCancelled = 1 THEN 1 ELSE 0 END, @TimeStamp) 
							, [Variant]
							, [XenAppName]
							,'Update', @TimeStamp 
							FROM inserted ap
							JOIN ApplicationPackageStatus aps ON ap.StatusId = aps.id


						END

						-- delete case
						IF NOT EXISTS (select * FROM inserted) AND EXISTS (SELECT * FROM deleted)
						BEGIN INSERT INTO LogApplicationPackages ( [AdGroup], [ApplicationId], [BuatAvailable], [CancelDate], [ClarificationDate], [DcPair], [DeploymentGroup], [HasDependencies], [HasUserConfigSettings], [HoldDate], [Id], [IsAvailable], [Modified], [ModifiedBy], [OnHoldBy], [PackageUseId], [PackagingCompleted], [PackagingStarted], [PackagingSubmitted], [ProductionAvailable], [StatusId], [Type], [UatAvailable], [UatSignedOff], [Variant], [XenAppName], [LogAction], [LogTime]) SELECT  [AdGroup], [ApplicationId], [BuatAvailable], [CancelDate], [ClarificationDate], [DcPair], [DeploymentGroup], [HasDependencies], [HasUserConfigSettings], [HoldDate], [Id], [IsAvailable], [Modified], [ModifiedBy], [OnHoldBy], [PackageUseId], [PackagingCompleted], [PackagingStarted], [PackagingSubmitted], [ProductionAvailable], [StatusId], [Type], [UatAvailable], [UatSignedOff], [Variant], [XenAppName],'Delete', @TimeStamp FROM deleted END

						END

						"""
            }
        };
    }
    
}