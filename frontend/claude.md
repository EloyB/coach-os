# CoachOS Frontend - Next.js 15

## Project Context

This is the frontend for CoachOS, a tennis/padel lesson planning SaaS for the Benelux market.

## Tech Stack

- **Framework:** Next.js 15 (App Router)
- **Language:** TypeScript
- **Package Manager:** Bun
- **UI Library:** Shadcn UI + Radix
- **Styling:** Tailwind CSS
- **State:** React Query (TanStack Query)
- **Forms:** React Hook Form + Zod
- **i18n:** next-intl
- **HTTP:** Axios (via api-client)

## Architecture Principles

### 1. Server Components First

- **DEFAULT:** Use Server Components (async, fetch data server-side)
- **ONLY** use Client Components when you need:
  - User interaction (onClick, onChange, etc.)
  - React hooks (useState, useEffect, etc.)
  - Browser APIs (localStorage, window, etc.)

### 2. App Router Structure

```
app/
├── (auth)/              # Auth routes (login, register)
│   └── login/
│       └── page.tsx
├── (dashboard)/         # Protected routes
│   ├── layout.tsx       # Dashboard shell
│   ├── page.tsx         # Dashboard home
│   └── courts/
│       └── page.tsx
└── api/                 # API routes (if needed)
```

### 3. Component Organization

```
components/
├── ui/                  # Shadcn components (auto-generated)
│   ├── button.tsx
│   ├── card.tsx
│   └── form.tsx
├── forms/               # Custom forms
│   └── create-court-form.tsx
├── layouts/             # Layout components
│   └── dashboard-layout.tsx
└── shared/              # Shared components
    └── loading-spinner.tsx
```

## Key Patterns

### Server Component (Data Fetching)

```tsx
// app/dashboard/courts/page.tsx
import { getCourts } from "@/lib/api/courts";

export default async function CourtsPage() {
  // Fetch server-side (no loading state needed!)
  const courts = await getCourts("org-id");

  return (
    <div>
      <h1>Banen</h1>
      <div className="grid gap-4">
        {courts.map((court) => (
          <CourtCard key={court.id} court={court} />
        ))}
      </div>
    </div>
  );
}
```

### Client Component (Interactive Form)

```tsx
// components/forms/create-court-form.tsx
"use client"; // REQUIRED for client components!

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import * as z from "zod";
import { Button } from "@/components/ui/button";
import { Form, FormField, FormItem, FormLabel } from "@/components/ui/form";

const formSchema = z.object({
  name: z.string().min(1, "Naam is verplicht").max(100),
  type: z.enum(["Tennis", "Padel"]),
});

type FormValues = z.infer<typeof formSchema>;

export function CreateCourtForm() {
  const t = useTranslations("courts");

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: "",
      type: "Tennis",
    },
  });

  async function onSubmit(data: FormValues) {
    try {
      await createCourt(data);
      // Success handling
    } catch (error) {
      // Error handling
    }
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t("name")}</FormLabel>
              <Input {...field} />
            </FormItem>
          )}
        />

        <Button type="submit">{t("create")}</Button>
      </form>
    </Form>
  );
}
```

### API Client Pattern

```typescript
// lib/api/courts.ts
import apiClient from "@/lib/api-client";

export interface Court {
  id: string;
  name: string;
  type: "Tennis" | "Padel";
}

export interface CreateCourtRequest {
  name: string;
  type: "Tennis" | "Padel";
  organizationId: string;
}

export async function getCourts(organizationId: string): Promise<Court[]> {
  const { data } = await apiClient.get<Court[]>("/courts", {
    params: { organizationId },
  });
  return data;
}

export async function createCourt(
  request: CreateCourtRequest,
): Promise<string> {
  const { data } = await apiClient.post<string>("/courts", request);
  return data;
}

export async function deleteCourt(id: string): Promise<void> {
  await apiClient.delete(`/courts/${id}`);
}
```

### React Query Pattern (Optional, for client-side data)

```tsx
"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getCourts, createCourt } from "@/lib/api/courts";

export function CourtsClientList() {
  const queryClient = useQueryClient();

  // Fetch data
  const { data: courts, isLoading } = useQuery({
    queryKey: ["courts", "org-id"],
    queryFn: () => getCourts("org-id"),
  });

  // Mutation
  const createMutation = useMutation({
    mutationFn: createCourt,
    onSuccess: () => {
      // Invalidate cache to refetch
      queryClient.invalidateQueries({ queryKey: ["courts"] });
    },
  });

  if (isLoading) return <div>Loading...</div>;

  return (
    <div>
      {courts?.map((court) => (
        <div key={court.id}>{court.name}</div>
      ))}
    </div>
  );
}
```

## i18n Pattern (MANDATORY)

**NEVER hardcode Dutch text!** Always use translations.

```tsx
// Component
import { useTranslations } from "next-intl";

export function MyComponent() {
  const t = useTranslations("courts"); // Namespace

  return (
    <div>
      <h1>{t("title")}</h1>
      <p>{t("description", { count: 5 })}</p>
    </div>
  );
}
```

```json
// messages/nl.json
{
  "courts": {
    "title": "Banen",
    "description": "Je hebt {count} banen",
    "create": "Nieuwe baan",
    "name": "Naam",
    "type": "Type"
  }
}
```

## Styling with Tailwind

**Use Tailwind utility classes (NOT custom CSS):**

```tsx
// ✅ GOOD
<div className="flex items-center gap-4 p-6 bg-white rounded-lg shadow">
  <h1 className="text-2xl font-bold text-green-600">CoachOS</h1>
</div>

// ❌ BAD (custom CSS)
<div style={{ display: 'flex', padding: '24px' }}>
  <h1 style={{ fontSize: '24px' }}>CoachOS</h1>
</div>
```

**Use Shadcn components:**

```tsx
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";

<Card>
  <CardHeader>
    <CardTitle>Baan 1</CardTitle>
  </CardHeader>
  <CardContent>
    <p>Tennis</p>
  </CardContent>
</Card>;
```

## Routing

```tsx
// Navigation
import Link from "next/link";

<Link href="/dashboard/courts">Banen</Link>;

// Programmatic navigation
import { useRouter } from "next/navigation";

const router = useRouter();
router.push("/dashboard/courts");
```

## Environment Variables

```typescript
// Access with NEXT_PUBLIC_ prefix
const apiUrl = process.env.NEXT_PUBLIC_API_URL;

// .env.local
NEXT_PUBLIC_API_URL=http://localhost:5000/api
NEXT_PUBLIC_APP_NAME=CoachOS
```

## File Naming

- **Pages:** `page.tsx` (not index.tsx)
- **Layouts:** `layout.tsx`
- **Components:** `kebab-case.tsx` (create-court-form.tsx)
- **Utils:** `kebab-case.ts` (api-client.ts)
- **Types:** `types.ts` or inline

## Mandatory Checks

Before committing ANY code, verify:

- [ ] NO hardcoded Dutch strings (use next-intl)
- [ ] Server Components by default (Client only when needed)
- [ ] Using Shadcn UI (not custom components)
- [ ] Tailwind for styling (not custom CSS)
- [ ] Zod for form validation
- [ ] TypeScript types defined
- [ ] API calls in /lib/api/ folder
- [ ] Error handling in forms
- [ ] Loading states for async operations

## Common Mistakes to Avoid

❌ **DON'T:**

- Hardcode text (use t() from next-intl)
- Use 'use client' everywhere (Server Components are default!)
- Create custom UI from scratch (use Shadcn)
- Put API calls in components (use /lib/api/)
- Use inline styles (use Tailwind)
- Forget TypeScript types
- Skip error handling

✅ **DO:**

- Use translations for ALL text
- Server Components by default
- Shadcn UI components
- API calls in dedicated files
- Tailwind utility classes
- Define TypeScript interfaces
- Handle errors gracefully
- Show loading states

## Tennis Branding

**Colors:**

```typescript
// tailwind.config.ts
colors: {
  tennis: {
    green: '#2D5016',
    lime: '#D0FF14',
    beige: '#E8DCC4'
  }
}
```

**Usage:**

```tsx
<div className="bg-tennis-green text-tennis-lime">CoachOS</div>
```

## References

- Full analysis: `/docs/project-analysis.md`
- Development guide: `/docs/development-guide.md`
- Root rules: `/.clinerules`
- Shadcn docs: https://ui.shadcn.com
- Next.js docs: https://nextjs.org/docs
