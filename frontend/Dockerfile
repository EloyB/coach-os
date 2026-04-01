FROM node:22-alpine AS base
WORKDIR /app

# Install dependencies
COPY package.json package-lock.json ./
RUN npm ci

# Copy source
COPY . .

# Set API URL to point to backend container
ENV NEXT_PUBLIC_API_URL=http://localhost:5142/api

EXPOSE 5317

CMD ["npx", "next", "dev", "--port", "5317", "--hostname", "0.0.0.0"]
