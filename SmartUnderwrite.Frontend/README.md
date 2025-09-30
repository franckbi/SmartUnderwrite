# SmartUnderwrite Frontend

React TypeScript frontend application for the SmartUnderwrite loan processing system.

## Features

- **Authentication**: JWT-based authentication with role-based access control
- **Protected Routes**: Role-based route protection (Admin, Underwriter, Affiliate)
- **Material-UI**: Modern UI components with responsive design
- **TypeScript**: Full type safety throughout the application
- **Axios Integration**: HTTP client with automatic token refresh

## Getting Started

### Prerequisites

- Node.js 20.19+ or 22.12+ (recommended)
- npm or yarn

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

The application will be available at `http://localhost:3000`.

### Build

```bash
npm run build
```

### Serve Built Files

```bash
npm run serve
```

## Project Structure

```
src/
├── components/          # Reusable UI components
│   ├── auth/           # Authentication components
│   ├── common/         # Common/shared components
│   └── layout/         # Layout components
├── contexts/           # React contexts
├── hooks/              # Custom React hooks
├── pages/              # Page components
├── services/           # API services
├── types/              # TypeScript type definitions
└── utils/              # Utility functions
```

## Authentication

The application uses JWT tokens for authentication with the following roles:

- **Admin**: Full system access
- **Underwriter**: Can review and make decisions on applications
- **Affiliate**: Can submit and view their own applications

## API Integration

The frontend communicates with the SmartUnderwrite API at `/api`. The API client automatically:

- Adds JWT tokens to requests
- Handles token refresh
- Redirects to login on authentication failures

## Environment Configuration

The application expects the API to be available at `/api` (configured via Vite proxy).

For production, ensure the API base URL is correctly configured.
