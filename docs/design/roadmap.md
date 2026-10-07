# Priorities for a personal device and home-server assistant

The product should answer: what was observed, why it matters, how confident the
answer is, and what the owner can do next. These priorities are planned work,
not claims about current capabilities.

1. **Trustworthy collection status.** Return explicit available, unavailable,
   permission-denied, unsupported, and failed outcomes per probe. Track freshness,
   partial runs, and errors. Never turn an inaccessible security log into zero
   failed logins. Resolve collector/analytics identifier mismatches before relying
   on derived security flags.
2. **A simple first review.** One summary tool with evidence, plain-language
   explanations, uncertainty, and three prioritized next steps. Let the owner
   choose personal-device or home-server context and expected services.
3. **Useful network context.** Live TCP peers/listeners are available without
   payload retention. Add process attribution when permitted, local UDP endpoints,
   IPv4/IPv6 exposure context, and optional bounded traffic summaries. Socket
   snapshots cannot establish initiation direction, per-peer byte counts, all
   transient traffic, or router/NAT exposure. Avoid automatic DNS/IP reputation
   lookups that disclose addresses to third parties.
4. **Actual security coverage.** Add read-only patch/update status, disk encryption,
   endpoint protection, remote-access configuration, and startup/persistence
   inventory. Record evidence and platform limitations instead of claiming a
   complete vulnerability scan. Restore testing and backup integrity need separate
   evidence from filesystem availability.
5. **Owner-defined baselines.** Distinguish expected services and connections from
   changes. Make retention, thresholds, and recovery goals explicit rather than
   imposing enterprise compliance targets on a home server. Optional history
   should have data minimization and a clear deletion control.
6. **Release confidence.** Run Linux and Windows CI on every change, test the
   published standalone and `dnx` package paths as ordinary users, verify archive checksums
   and add provenance, and keep dependencies patched. Extend to ARM64 only after
   testing on representative devices.

7. **Storage privacy.** Enforce private data-directory permissions on each OS, define encryption requirements, and document deletion limits for SQLite pages, WAL files, backups, and client history. Add a storage size limit and validate retention while running and after restarts.

Automatic fixes, broad administrative privileges, remote shell access, packet
payload storage, and HTTP transport are outside the supported design.
