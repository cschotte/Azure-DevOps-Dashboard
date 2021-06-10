# Azure DevOps Dashboard

This solution generates a simple overview of all the [Azure DevOps](https://dev.azure.com/) projects in your organization and calculates the last know activity date on changes in commits, work items and the project itself.

## The architecture

The solution runs in as a single [Azure Web App](https://azure.microsoft.com/en-us/services/app-service/web/), it uses a background [WebJob](https://docs.microsoft.com/en-us/azure/app-service/webjobs-create) to collect all the data needed to present in the web dashboard. If you have many DevOps project (more then 300) in a single Azure DevOps organization, it is recommended to move them to a multiple organization structure to avoid any performance issues with Azure DevOps.

The WebJob uses an Azure DevOps personal access token (PAT) to communicate, it needs only read access. See here [how to get a personal access token](https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=preview-page).

![Architecture](Architecture/architecture.png)

You can also run the WebJob locally, set the following two environment variable first `azDevOpsUri` and `azDevOpsPat`:

```cmd
SET azDevOpsPat=tjqp44k54nqfmppaqd7di27kpvh...........
SET azDevOpsUri=https://dev.azure.com/yourorgname.....
```
