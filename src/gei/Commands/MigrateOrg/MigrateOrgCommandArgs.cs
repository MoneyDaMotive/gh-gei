﻿using OctoshiftCLI.Commands;

namespace OctoshiftCLI.GithubEnterpriseImporter.Commands.MigrateOrg
{
    public class MigrateOrgCommandArgs : CommandArgs
    {
        public string GithubSourceOrg { get; set; }
        public string GithubTargetOrg { get; set; }
        public string GithubTargetEnterprise { get; set; }
        public bool Wait { get; set; }
        public bool QueueOnly { get; set; }
        public string GithubSourcePat { get; set; }
        public string GithubTargetPat { get; set; }
    }
}
