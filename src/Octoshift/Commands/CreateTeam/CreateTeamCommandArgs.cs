﻿namespace OctoshiftCLI.Commands.CreateTeam;

public class CreateTeamCommandArgs : CommandArgs
{
    public string GithubOrg { get; set; }
    public string TeamName { get; set; }
    public string IdpGroup { get; set; }
    public string GithubPat { get; set; }
}
