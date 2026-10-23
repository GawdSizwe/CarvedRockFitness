# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in this project, please report it privately rather than
opening a public GitHub issue.

- Open a [GitHub Security Advisory](../../security/advisories/new) for this repository (preferred), or
- Email the maintainer listed on the repository's GitHub profile.

Please include:
- A description of the vulnerability and its potential impact.
- Steps to reproduce (a minimal proof-of-concept is ideal).
- Any suggested remediation, if you have one.

We'll acknowledge reports as quickly as possible and keep you updated as the issue is triaged and
fixed. Please allow a reasonable amount of time for a fix to be released before any public
disclosure.

## Supported Versions

This project tracks a single `main` branch deployed continuously; only the latest commit is
supported. There are no maintained release branches.

## Scope

In scope:
- `CarvedRockFitness.API`, `CarvedRockFitness.Auth`, `CarvedRockFitness.Web` and their
  dependencies as declared in this repository.

Out of scope:
- Third-party services this project integrates with (PayFast, Fly.io, OpenStreetMap/Nominatim,
  CDN-hosted libraries) — please report issues with those directly to their respective vendors.
- Denial-of-service testing against the live deployment.
