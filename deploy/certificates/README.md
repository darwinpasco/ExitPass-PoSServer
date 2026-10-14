# HTTPS certificate input

The deployment expects an externally issued PEM certificate chain and matching PEM private key. Do not place either file in this directory or commit them to Git.

The certificate must:

- contain the approved POS Server DNS name in its Subject Alternative Name;
- be valid for TLS server authentication;
- chain to a CA trusted by Central PMS and approved operators;
- be current for the complete demonstration or production window; and
- have a documented renewal and rollback owner.

Set `POSSERVER_TLS_CERT_FILE` and `POSSERVER_TLS_KEY_FILE` in the external deployment environment file. The files are mounted read-only into the API container. Restrict the private key with Windows ACLs and verify host-volume encryption before deployment.
