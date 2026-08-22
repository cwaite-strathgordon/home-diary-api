# HomeDiary invitation email

The API sends branded invitations through the Amazon SES v2 API in `eu-west-2`.
It does not use SMTP credentials. AWS credentials are resolved using the normal
AWS SDK chain (environment, named profile, workload role or instance role).

## AWS setup

1. In SES `eu-west-2`, verify `homediary.app` as a sending identity and publish
   its DKIM CNAME records in Route 53.
2. If the SES account is in the sandbox, request production access. In the
   sandbox every invitation recipient must also be verified.
3. Attach `iam-ses-invitations-policy.json` to the local IAM user or production
   workload role used by the HomeDiary API. Both `ses:SendEmail` and
   `ses:SendRawEmail` are required because the branded message and inline logo
   are sent as raw MIME content through the SES v2 API. The policy uses a `*`
   resource because SES can authorize raw sends against recipient identities as
   well as the sending identity; the `ses:FromAddress` condition still limits
   the permission to `no-reply@homediary.app`.
4. For local development, run the API with a profile that has this policy:

   ```bash
   AWS_PROFILE=homediary-api dotnet run
   ```

## Application configuration

Production defaults to:

```json
{
  "InvitationEmail": {
    "Region": "eu-west-2",
    "FromAddress": "no-reply@homediary.app",
    "FromName": "HomeDiary",
    "ApplicationBaseUrl": "https://homediary.app"
  }
}
```

Development overrides the application URL with `http://localhost:4200`.
Environment variables can override any value, for example:

```bash
InvitationEmail__ApplicationBaseUrl=https://homediary.app
```

Apply database migrations 019 and 020 before starting the updated API.
