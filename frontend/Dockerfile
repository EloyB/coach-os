FROM node:24-alpine AS base
WORKDIR /app

# Bun as package manager (lockfile is bun.lock). Same version as CI
# (.github/workflows/pr-check-frontend.yml); Node stays the runtime for Next.
COPY --from=oven/bun:1.3.13-alpine /usr/local/bin/bun /usr/local/bin/bun
RUN ln -s /usr/local/bin/bun /usr/local/bin/bunx

# Install dependencies
COPY package.json bun.lock ./
RUN bun install --frozen-lockfile

# Copy source
COPY . .

# Set API URL to point to backend container
ENV NEXT_PUBLIC_API_URL=http://localhost:5142/api

EXPOSE 5317

CMD ["node_modules/.bin/next", "dev", "--port", "5317", "--hostname", "0.0.0.0"]
