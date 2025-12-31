# Remove previous directories
rm -rf coverage-report TestResults

# Run all tests with coverage
dotnet test --settings:coverage.runsettings --results-directory ./TestResults

# Generate HTML coverage report
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# View it in browser
start coverage-report/index.html