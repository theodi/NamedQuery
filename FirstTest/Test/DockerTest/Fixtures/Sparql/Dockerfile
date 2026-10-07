FROM ghcr.io/oxigraph/oxigraph:0.5.11
COPY data.nt /data.nt
RUN ["oxigraph", "load", "--location", "/data", "--file", "/data.nt"]
