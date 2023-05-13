﻿using System.Linq;
using OctoshiftCLI.Commands;
using OctoshiftCLI.Extensions;
using OctoshiftCLI.Services;

namespace OctoshiftCLI.BbsToGithub.Commands.MigrateRepo;

public class MigrateRepoCommandArgs : CommandArgs
{
    public string ArchiveUrl { get; set; }
    public string ArchivePath { get; set; }

    [Secret]
    public string AzureStorageConnectionString { get; set; }

    public string AwsBucketName { get; set; }
    [Secret]
    public string AwsAccessKey { get; set; }
    [Secret]
    public string AwsSecretKey { get; set; }
    [Secret]
    public string AwsSessionToken { get; set; }
    public string AwsRegion { get; set; }

    public string GithubOrg { get; set; }
    public string GithubRepo { get; set; }
    [Secret]
    public string GithubPat { get; set; }
    public bool Wait { get; set; }
    public bool QueueOnly { get; set; }
    public string TargetRepoVisibility { get; set; }
    public bool Kerberos { get; set; }

    public string BbsServerUrl { get; set; }
    public string BbsProject { get; set; }
    public string BbsRepo { get; set; }
    public string BbsUsername { get; set; }
    [Secret]
    public string BbsPassword { get; set; }
    public string BbsSharedHome { get; set; }
    public bool NoSslVerify { get; set; }

    public string ArchiveDownloadHost { get; set; }
    public string SshUser { get; set; }
    public string SshPrivateKey { get; set; }
    public int SshPort { get; set; } = 22;

    public string SmbUser { get; set; }
    [Secret]
    public string SmbPassword { get; set; }
    public string SmbDomain { get; set; }

    public bool KeepArchive { get; set; }

    public override void Validate(OctoLogger log)
    {
        if (!BbsServerUrl.HasValue() && !ArchiveUrl.HasValue() && !ArchivePath.HasValue())
        {
            throw new OctoshiftCliException("Either --bbs-server-url, --archive-path, or --archive-url must be specified.");
        }

        if (BbsServerUrl.HasValue() && ArchiveUrl.HasValue())
        {
            throw new OctoshiftCliException("Only one of --bbs-server-url or --archive-url can be specified.");
        }

        if (BbsServerUrl.HasValue() && ArchivePath.HasValue())
        {
            throw new OctoshiftCliException("Only one of --bbs-server-url or --archive-path can be specified.");
        }

        if (ArchivePath.HasValue() && ArchiveUrl.HasValue())
        {
            throw new OctoshiftCliException("Only one of --archive-path or --archive-url can be specified.");
        }

        if (ShouldGenerateArchive())
        {
            ValidateGenerateOptions();
            ValidateDownloadOptions();
        }
        else
        {
            ValidateNoGenerateOptions();
        }

        if (ShouldUploadArchive())
        {
            ValidateUploadOptions();
        }

        if (ShouldImportArchive())
        {
            ValidateImportOptions();
        }

        if (Wait)
        {
            log?.LogWarning("--wait flag is obsolete and will be removed in a future version. The default behavior is now to wait.");
        }

        if (Wait && QueueOnly)
        {
            throw new OctoshiftCliException("You can't specify both --wait and --queue-only at the same time.");
        }

        if (!Wait && !QueueOnly)
        {
            log?.LogWarning("The default behavior has changed from only queueing the migration, to waiting for the migration to finish. If you ran this as part of a script to run multiple migrations in parallel, consider using the new --queue-only option to preserve the previous default behavior. This warning will be removed in a future version.");
        }
    }

    private void ValidateNoGenerateOptions()
    {
        if (BbsUsername.HasValue() || BbsPassword.HasValue())
        {
            throw new OctoshiftCliException("--bbs-username and --bbs-password can only be provided with --bbs-server-url.");
        }

        if (NoSslVerify)
        {
            throw new OctoshiftCliException("--no-ssl-verify can only be provided with --bbs-server-url.");
        }

        if (BbsProject.HasValue() || BbsRepo.HasValue())
        {
            throw new OctoshiftCliException("--bbs-project and --bbs-repo can only be provided with --bbs-server-url.");
        }

        if (new[] { SshUser, SshPrivateKey, ArchiveDownloadHost, SmbUser, SmbPassword, SmbDomain }.Any(obj => obj.HasValue()))
        {
            throw new OctoshiftCliException("SSH or SMB download options can only be provided with --bbs-server-url.");
        }
    }

    public bool ShouldGenerateArchive() => BbsServerUrl.HasValue();

    public bool ShouldDownloadArchive() => SshUser.HasValue() || SmbUser.HasValue();

    public bool ShouldUploadArchive() => ArchiveUrl.IsNullOrWhiteSpace() && GithubOrg.HasValue();

    public bool ShouldImportArchive() => ArchiveUrl.HasValue() || GithubOrg.HasValue();

    private void ValidateGenerateOptions()
    {
        if (Kerberos)
        {
            if (BbsUsername.HasValue() || BbsPassword.HasValue())
            {
                throw new OctoshiftCliException("--bbs-username and --bbs-password cannot be provided with --kerberos.");
            }
        }

        if (BbsProject.IsNullOrWhiteSpace() || BbsRepo.IsNullOrWhiteSpace())
        {
            throw new OctoshiftCliException("Both --bbs-project and --bbs-repo must be provided.");
        }
    }

    private void ValidateDownloadOptions()
    {
        var sshArgs = new[] { SshUser, SshPrivateKey };
        var smbArgs = new[] { SmbUser, SmbPassword };
        var shouldUseSsh = sshArgs.Any(arg => arg.HasValue());
        var shouldUseSmb = smbArgs.Any(arg => arg.HasValue());

        if (shouldUseSsh && shouldUseSmb)
        {
            throw new OctoshiftCliException("You can't provide both SSH and SMB credentials together.");
        }

        if (SshUser.HasValue() ^ SshPrivateKey.HasValue())
        {
            throw new OctoshiftCliException("Both --ssh-user and --ssh-private-key must be specified for SSH download.");
        }

        if (ArchiveDownloadHost.HasValue() && !shouldUseSsh && !shouldUseSmb)
        {
            throw new OctoshiftCliException("--archive-download-host can only be provided if SSH or SMB download options are provided.");
        }
    }

    private void ValidateUploadOptions()
    {
        if (AwsBucketName.IsNullOrWhiteSpace() && new[] { AwsAccessKey, AwsSecretKey, AwsSessionToken, AwsRegion }.Any(x => x.HasValue()))
        {
            throw new OctoshiftCliException("The AWS S3 bucket name must be provided with --aws-bucket-name if other AWS S3 upload options are set.");
        }
    }

    private void ValidateImportOptions()
    {
        if (GithubOrg.IsNullOrWhiteSpace())
        {
            throw new OctoshiftCliException("--github-org must be provided in order to import the Bitbucket archive.");
        }

        if (GithubRepo.IsNullOrWhiteSpace())
        {
            throw new OctoshiftCliException("--github-repo must be provided in order to import the Bitbucket archive.");
        }
    }
}
