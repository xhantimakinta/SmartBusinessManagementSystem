# Contributing

Thank you for considering a contribution to SmartBusiness Management System.

## Development Workflow

1. Create a feature branch from the main branch.
2. Keep changes focused and scoped to a single task.
3. Add or update tests for behavioral changes.
4. Run the relevant validation commands before opening a pull request.
5. Submit a pull request with a clear summary of the change and verification performed.

## Validation

Use the project checks before submitting changes:

- dotnet test SmartBusiness.slnx --nologo
- cd frontend && npm test -- --run
- cd frontend && npm run lint

## Pull Request Expectations

- include a concise description of the issue or feature
- describe the verification steps taken
- note any follow-up considerations or risks

## Code Standards

- keep code readable and maintainable
- avoid unnecessary dependencies
- prefer clear names and consistent formatting
- do not add secrets directly to source-controlled files
