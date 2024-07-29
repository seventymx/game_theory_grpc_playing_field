# Main Service - `playing_field`

This service is the main orchestrator, handling strategy subscriptions and running matchups. It's available over gRPC and gRPC-Web.

```powershell
# Run the main service
dotnet run
```

## Building the Project with Nix

To build the project using Nix, you can run the following command:

```sh
nix build --option sandbox false
```

### Note:

Disabling the sandbox is necessary for this build because the `dotnet` command needs network access to download NuGet packages.
While this approach works for now, we are exploring more elegant solutions to handle dependencies in a sandboxed environment.
This includes pre-fetching dependencies or using local caches to ensure a secure and reproducible build process.
