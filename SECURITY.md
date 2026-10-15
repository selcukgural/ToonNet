# Security Policy

## Supported Versions

Fixes are released for the latest major version.

| Version | Supported |
|---------|-----------|
| 4.x     | Yes |
| < 4.0   | No; upgrade to 4.x (see the migration guide) |

## Reporting a Vulnerability

Please do not open a public issue for a security problem. Report it privately through GitHub:
**Security** tab of this repository → **Report a vulnerability**
([private vulnerability reporting](https://github.com/selcukgural/ToonNet/security/advisories/new)).

Include what you can of:

- the package and version, and the target framework
- the input, code or configuration that shows the problem (a minimal document or snippet)
- what happens (a crash, a slow or large result, a value that should not be there) and what you expected

You will get an answer as soon as possible. A confirmed problem is fixed in a release before it is described in public,
and the advisory credits the reporter unless they ask otherwise.

## Scope

ToonNet parses documents from untrusted sources (request bodies, files, answers of language models), so problems such
as crashes, unbounded time or memory, structure injected through written strings, values set or written that the
types do not allow, and data in exception messages are in scope. The
[security guide](https://selcukgural.github.io/ToonNet/docs/advanced/security) lists the limits and safe defaults.
