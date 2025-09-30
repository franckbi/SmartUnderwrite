#!/bin/bash

# GitHub Actions Workflow Validation Script
# This script validates the GitHub Actions workflows for syntax and best practices

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}GitHub Actions Workflow Validation${NC}"
echo ""

# Check if GitHub CLI is installed
if ! command -v gh &> /dev/null; then
    echo -e "${YELLOW}GitHub CLI not installed. Skipping workflow validation.${NC}"
    echo "Install with: brew install gh"
    exit 0
fi

# Check if we're in a git repository
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo -e "${RED}Not in a git repository${NC}"
    exit 1
fi

# Validate workflow files
validate_workflows() {
    echo -e "${YELLOW}Validating workflow files...${NC}"
    
    WORKFLOW_DIR=".github/workflows"
    
    if [ ! -d "$WORKFLOW_DIR" ]; then
        echo -e "${RED}No workflows directory found${NC}"
        exit 1
    fi
    
    # Check each workflow file
    for workflow in "$WORKFLOW_DIR"/*.yml; do
        if [ -f "$workflow" ]; then
            filename=$(basename "$workflow")
            echo -n "Validating $filename... "
            
            # Basic YAML syntax check
            if python3 -c "import yaml; yaml.safe_load(open('$workflow'))" 2>/dev/null; then
                echo -e "${GREEN}✓${NC}"
            else
                echo -e "${RED}✗ (YAML syntax error)${NC}"
                exit 1
            fi
        fi
    done
}

# Check for required secrets and variables
check_secrets() {
    echo -e "${YELLOW}Checking required secrets and variables...${NC}"
    
    # Required secrets
    REQUIRED_SECRETS=(
        "AWS_ROLE_ARN"
        "JWT_SECRET"
    )
    
    echo "Required repository secrets:"
    for secret in "${REQUIRED_SECRETS[@]}"; do
        echo "  - $secret"
    done
    
    echo ""
    echo "To set secrets, run:"
    echo "  gh secret set AWS_ROLE_ARN --body 'arn:aws:iam::ACCOUNT:role/ROLE'"
    echo "  gh secret set JWT_SECRET --body 'your-secure-jwt-secret'"
}

# Check environment configuration
check_environments() {
    echo -e "${YELLOW}Checking environment configuration...${NC}"
    
    ENVIRONMENTS=("dev" "prod")
    
    for env in "${ENVIRONMENTS[@]}"; do
        echo "Environment: $env"
        echo "  Configuration file: .github/environments/$env.yml"
        
        if [ -f ".github/environments/$env.yml" ]; then
            echo -e "  Status: ${GREEN}✓ Found${NC}"
        else
            echo -e "  Status: ${YELLOW}⚠ Configuration file missing${NC}"
        fi
    done
    
    echo ""
    echo "Environment setup instructions:"
    echo "1. Go to repository Settings > Environments"
    echo "2. Create 'dev' and 'prod' environments"
    echo "3. Configure protection rules for 'prod' environment"
    echo "4. Add environment-specific secrets if needed"
}

# Validate Terraform configuration
validate_terraform() {
    echo -e "${YELLOW}Validating Terraform configuration...${NC}"
    
    if [ ! -d "terraform" ]; then
        echo -e "${RED}Terraform directory not found${NC}"
        return 1
    fi
    
    cd terraform
    
    # Check if Terraform is installed
    if ! command -v terraform &> /dev/null; then
        echo -e "${YELLOW}Terraform not installed. Skipping validation.${NC}"
        cd ..
        return 0
    fi
    
    # Initialize and validate
    echo "Initializing Terraform..."
    if terraform init -backend=false > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Terraform init successful${NC}"
    else
        echo -e "${RED}✗ Terraform init failed${NC}"
        cd ..
        return 1
    fi
    
    echo "Validating Terraform configuration..."
    if terraform validate > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Terraform validation successful${NC}"
    else
        echo -e "${RED}✗ Terraform validation failed${NC}"
        terraform validate
        cd ..
        return 1
    fi
    
    cd ..
}

# Check Docker configuration
validate_docker() {
    echo -e "${YELLOW}Validating Docker configuration...${NC}"
    
    DOCKERFILE="SmartUnderwrite.Api/Dockerfile"
    
    if [ -f "$DOCKERFILE" ]; then
        echo -e "Dockerfile: ${GREEN}✓ Found${NC}"
        
        # Basic Dockerfile validation
        if grep -q "FROM" "$DOCKERFILE" && grep -q "EXPOSE" "$DOCKERFILE"; then
            echo -e "Dockerfile syntax: ${GREEN}✓ Basic validation passed${NC}"
        else
            echo -e "Dockerfile syntax: ${YELLOW}⚠ Missing required instructions${NC}"
        fi
    else
        echo -e "Dockerfile: ${RED}✗ Not found${NC}"
    fi
}

# Show workflow status
show_workflow_status() {
    echo -e "${YELLOW}Recent workflow runs:${NC}"
    
    if gh run list --limit 5 > /dev/null 2>&1; then
        gh run list --limit 5
    else
        echo "Unable to fetch workflow runs. Make sure you're authenticated with 'gh auth login'"
    fi
}

# Main execution
main() {
    validate_workflows
    echo ""
    
    check_secrets
    echo ""
    
    check_environments
    echo ""
    
    validate_terraform
    echo ""
    
    validate_docker
    echo ""
    
    show_workflow_status
    echo ""
    
    echo -e "${GREEN}Validation complete!${NC}"
    echo ""
    echo "Next steps:"
    echo "1. Configure required secrets in GitHub repository settings"
    echo "2. Set up dev and prod environments with appropriate protection rules"
    echo "3. Test workflows by creating a pull request or pushing to main branch"
    echo "4. Monitor workflow runs in the Actions tab"
}

main