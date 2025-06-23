# BrokenStats Backend

This is a small backend service used to collect and serve fight data.

## Running

Start the API with:

```bash
dotnet run
```

It listens on `http://localhost:5005` and automatically opens the dashboard in your default browser.

## Environment

The application checks the `ASPNETCORE_ENVIRONMENT` variable to determine the environment. When set to `Development`, `UseDeveloperExceptionPage()` is invoked which shows detailed error pages and enables more verbose logging. In other environments the generic `/error` endpoint is used.

