# Frontend

This folder contains a very small web page used to view data from the backend.

## Running

1. Start the backend:
   ```bash
   dotnet run
   ```
   The API listens on `http://localhost:5005`.

2. Open a browser and navigate to `http://localhost:5005/`.
   The `index.html` page will request data from `/api/fights/flat` and `/api/fights/summary` and show the results.

Use the form at the top of the page to change the date range or paging options and click **Load** to refresh the data.
